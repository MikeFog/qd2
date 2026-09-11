#!/usr/bin/env bash
#
# Сверка вывода отчётных процедур между двумя копиями базы:
#   OLD -- копия до миграции (старые процедуры, старый Campaign.finalPrice)
#   NEW -- копия после campaign-finalprice-with-pack-deploy.sql
#
# Один и тот же EXEC гоняется в обеих базах, строки сортируются и сравниваются.
# Сортировка -- потому что часть процедур не имеет ORDER BY.
#
# ВАЖНО про кодировку: вывод только через "-o файл -u" (UTF-16). При простом
# перенаправлении stdout sqlcmd отдаёт разную кодировку в разных сессиях, и
# diff показывает расхождение там, где данные совпадают.
#
# ЗАПУСК
#   bash ArtvisDB/Scripts/campaign-finalprice-with-pack-compare.sh [OLD] [NEW] [SERVER]
#
# ЧТО СЧИТАТЬ НОРМОЙ
#   Campaigns          -- отличается ровно одна колонка, finalPrice: её мы и меняли.
#                         Всё остальное (fullPrice, price, packDiscount, скидки)
#                         обязано совпасть до копейки.
#   stat_VolumeOf*     -- расхождения в доли копейки на строку: старый код
#                         суммировал неокруглённые произведения.
#   ActionsForPayment* -- список должников меняется: уходят акции с нулевым
#                         долгом (их показывали ошибочно), приходят акции с
#                         долгом 1-2 копейки.
#   Всё остальное      -- обязано совпасть полностью.
#
set -u

OLD="${1:-Artvis}"
NEW="${2:-Artvis2}"
SRV="${3:-.\sqlexpress}"
OUT="${OUT:-tmp_compare}"
U="${LOGGED_USER_ID:-0}"
mkdir -p "$OUT"

S=20260701                # квартал -- чтобы тяжёлые отчёты укладывались в разумное время
F=20260930
SW=20260101               # год
FW=20260930

run_db() {  # $1=db  $2=sqlfile  $3=outfile
    sqlcmd -S "$SRV" -d "$1" -E -I -W -s '|' -i "$2" -o "$3.u16" -u
    iconv -f UTF-16 -t UTF-8 "$3.u16" > "$3" 2>/dev/null || cp "$3.u16" "$3"
    rm -f "$3.u16"
}

compare() {  # $1=имя
    local n="$1" rows d
    run_db "$OLD" "$OUT/$n.sql" "$OUT/$n.old"
    run_db "$NEW" "$OUT/$n.sql" "$OUT/$n.new"
    sort "$OUT/$n.old" > "$OUT/$n.old.s"
    sort "$OUT/$n.new" > "$OUT/$n.new.s"
    rows=$(( $(wc -l < "$OUT/$n.old") - 2 ))
    if cmp -s "$OUT/$n.old.s" "$OUT/$n.new.s"; then
        # Одинаковая ошибка в обеих базах -- дефект самой процедуры, к правке
        # отношения не имеет: помечаем отдельно, но провалом не считаем.
        if grep -qE 'HResult|Msg [0-9]|уровень 16' "$OUT/$n.old" 2>/dev/null; then
            printf '%-24s %8s  %s
' "$n" "-" "обе базы падают одинаково (дефект не от правки)"
            head -2 "$OUT/$n.old" | tail -1 | sed 's/^/      /'
        else
            printf '%-24s %8s  %s
' "$n" "$rows" "совпало"
        fi
    else
        d=$(diff "$OUT/$n.old.s" "$OUT/$n.new.s" | grep -c '^[<>]')
        printf '%-24s %8s  %s
' "$n" "$rows" "РАСХОЖДЕНИЕ: $d строк"
        return 1
    fi
}

echo "OLD=$OLD  NEW=$NEW  сервер=$SRV  пользователь=$U"
echo
echo "--- часть 1: отчёты целиком ---"
printf '%-24s %8s  %s
' "случай" "строк" "результат"

declare -a NAME SQL
add() { NAME+=("$1"); SQL+=("$2"); }

add balance_all        "EXEC dbo.stat_Balance @loggedUserID=$U"
add balance_date       "EXEC dbo.stat_Balance @theDate='$F', @loggedUserID=$U"
add balance_by_agency  "EXEC dbo.stat_Balance @theDate='$F', @IsGroupByAgency=1, @loggedUserID=$U"
add balance_agency     "EXEC dbo.stat_BalanceAgency @theDate='$F', @loggedUserID=$U"
add balance_manager    "EXEC dbo.stat_BalanceManager @theDate='$F', @loggedUserID=$U"
add actions_balance    "EXEC dbo.ActionsForBalance @loggedUserID=$U"
add actions_balance_per "EXEC dbo.ActionsForBalance @startOfInterval='$S', @endOfInterval='$F', @loggedUserID=$U"
add volumes_by_pt      "EXEC dbo.stat_VolumesByPaymentTypes @startDate='$SW', @finishDate='$FW', @loggedUserID=$U"
add volumes_by_pt_grp  "EXEC dbo.stat_VolumesByPaymentTypes @startDate='$SW', @finishDate='$FW', @groupByPaymentType=1, @loggedUserID=$U"
add vol_realization    "EXEC dbo.stat_VolumeOfRealization @StartDay='$S', @FinishDay='$F', @IsGroupByMassmedia=1, @IsGroupByFirm=1, @loggedUserID=$U"
add vol_realization_new "EXEC dbo.stat_VolumeOfRealizationNew @StartDay='$S', @FinishDay='$F', @IsGroupByMassmedia=1, @IsGroupByAdvertType=1, @loggedUserID=$U"
add vol_by_month       "EXEC dbo.stat_VolumeOfRealizationByMonth @StartDay='$S', @FinishDay='$F', @IsGroupByMassmedia=1, @loggedUserId=$U"
add vol_by_month_firm  "EXEC dbo.stat_VolumeOfRealizationByMonth @StartDay='$S', @FinishDay='$F', @IsGroupByFirm=1, @IsGroupByManager=1, @loggedUserId=$U"
add avg_discount       "EXEC dbo.Stat_AvgDiscount @StartDay='$S', @FinishDay='$F', @IsGroupByMassmedia=1, @loggedUserID=$U"
add avg_discount_mgr   "EXEC dbo.Stat_AvgDiscount @StartDay='$S', @FinishDay='$F', @IsGroupByManager=1, @IsGroupByCampaignType=1, @loggedUserID=$U"
add bonuses            "EXEC dbo.stat_Bonuses @periodStartDate='$SW', @periodFinishDate='$FW'"
add bonuses_bycreate   "EXEC dbo.stat_Bonuses @periodStartDate='$SW', @periodFinishDate='$FW', @selectByCreateDate=1"
add bonuses_byhead     "EXEC dbo.stat_Bonuses @periodStartDate='$SW', @periodFinishDate='$FW', @isGroupByFirm=0"
add factor_analysis    "EXEC dbo.statFactorAnalysis @StartDay='$S', @FinishDay='$F', @ComparedStartDay='20250701', @IsGroupByMassmedia=1, @loggedUserID=$U"
add factor_analysis_firm "EXEC dbo.statFactorAnalysis @StartDay='$S', @FinishDay='$F', @ComparedStartDay='20250701', @IsGroupByFirm=1, @loggedUserID=$U"
# у журнала актов агентство обязательно
AG=$(sqlcmd -S "$SRV" -d "$OLD" -E -I -h -1 -W -Q "SET NOCOUNT ON; SELECT TOP 1 c.agencyID FROM dbo.Campaign c GROUP BY c.agencyID ORDER BY COUNT(*) DESC" | tr -d "[:space:]")
add act_journal        "EXEC dbo.CampaignsForActJournalRetrieve @startDate='$SW', @finishDate='$FW', @agencyID=$AG, @loggedUserID=$U"

fail=0
for i in "${!NAME[@]}"; do
    n="${NAME[$i]}"
    printf 'SET NOCOUNT ON;
%s
' "${SQL[$i]}" > "$OUT/$n.sql"
    compare "$n" || fail=$((fail+1))
done

echo
echo "--- часть 2: процедуры, вызываемые по одному объекту ---"

gen() {   # $1=имя  $2=sql, печатающий строки "EXEC ..."
    local n="$1"
    sqlcmd -S "$SRV" -d "$OLD" -E -I -h -1 -W -Q "SET NOCOUNT ON; $2"         | sed 's/[[:space:]]*$//' | grep -v '^$' > "$OUT/$n.gen"
    { echo 'SET NOCOUNT ON;'; cat "$OUT/$n.gen"; } > "$OUT/$n.sql"
    rm -f "$OUT/$n.gen"
}

gen campaigns     "SELECT TOP 300 'EXEC dbo.Campaigns @actionID=' + CONVERT(varchar(12), actionID) + ', @loggedUserID=$U' FROM dbo.[Action] ORDER BY actionID DESC"
compare campaigns || fail=$((fail+1))

gen campaigns_mm  "SELECT 'EXEC dbo.Campaigns @massmediaID=' + CONVERT(varchar(12), massmediaID) + ', @loggedUserID=$U' FROM dbo.MassMedia"
compare campaigns_mm || fail=$((fail+1))

gen payments      "SELECT TOP 300 'EXEC dbo.ActionsForPaymentCommon @paymentID=' + CONVERT(varchar(12), paymentID) + ', @loggedUserID=$U' FROM dbo.Payment ORDER BY paymentID DESC"
compare payments || fail=$((fail+1))

gen balance_firm  "SELECT TOP 200 'EXEC dbo.ActionsForBalance @firmID=' + CONVERT(varchar(12), a.firmID) + ', @loggedUserID=$U' FROM (SELECT DISTINCT firmID FROM dbo.[Action]) a ORDER BY a.firmID DESC"
compare balance_firm || fail=$((fail+1))

echo
if [ "$fail" -eq 0 ]; then
    echo "ИТОГ: расхождений нет."
else
    echo "ИТОГ: расхождения в $fail случаях. Разбирать: $OUT/<случай>.{old,new}.s"
    echo "Сверяйтесь с разделом \"ЧТО СЧИТАТЬ НОРМОЙ\" в шапке скрипта."
fi
