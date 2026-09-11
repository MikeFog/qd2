# -*- coding: utf-8 -*-
"""
Пересобирает campaign-finalprice-with-pack-deploy.sql из исходников процедур.

Деплой-скрипт содержит тела 17 процедур; редактировать их там руками нельзя --
разъедется с ArtvisDB/dbo/Stored Procedures/*.sql. После правки любой из этих
процедур запустить:

    python ArtvisDB/Scripts/campaign-finalprice-with-pack-gen.py

Шапка и хвост скрипта живут в самом файле деплоя между маркерами
-- @@HEAD-END@@ и -- @@TAIL-BEGIN@@ и при пересборке сохраняются.

QUOTED_IDENTIFIER / ANSI_NULLS для каждой процедуры зафиксированы такими, с
какими объект развёрнут на проде (sys.sql_modules): менять их заодно с правкой
тела -- лишний риск.
"""
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, "ArtvisDB", "dbo", "Stored Procedures")
OUT = os.path.join(ROOT, "ArtvisDB", "Scripts", "campaign-finalprice-with-pack-deploy.sql")

HEAD_END = "-- @@HEAD-END@@"
TAIL_BEGIN = "-- @@TAIL-BEGIN@@"

# имя -> (QUOTED_IDENTIFIER, ANSI_NULLS)
PROCS = [
    ("ActionRecalculate",               1, 1),
    ("CampaignSetFinalPrice",           1, 1),
    ("Campaigns",                       0, 1),
    ("ActionsForBalance",               1, 1),
    ("ActionsForPaymentCommon",         1, 1),
    ("CampaignsForActJournalRetrieve",  1, 1),
    ("job_DeleteHistory",               1, 1),
    ("Stat_AvgDiscount",                1, 1),
    ("stat_Balance",                    0, 1),
    ("stat_BalanceAgency",              0, 0),
    ("stat_BalanceManager",             1, 1),
    ("stat_Bonuses",                    1, 1),
    ("statFactorAnalysis",              1, 1),
    ("stat_VolumeOfRealization",        1, 1),
    ("stat_VolumeOfRealizationByMonth", 1, 1),
    ("stat_VolumeOfRealizationNew",     1, 1),
    ("stat_VolumesByPaymentTypes",      1, 1),
]

CREATE_RE = re.compile(r"^(\s*)CREATE(\s+)(PROCEDURE|PROC)\b", re.IGNORECASE | re.MULTILINE)


def main():
    if not os.path.exists(OUT):
        sys.exit("нет %s -- пересобирать нечего" % OUT)

    current = io.open(OUT, encoding="utf-8-sig").read()
    if HEAD_END not in current or TAIL_BEGIN not in current:
        sys.exit("в %s нет маркеров %s / %s" % (OUT, HEAD_END, TAIL_BEGIN))

    head = current.split(HEAD_END)[0] + HEAD_END + "\n"
    tail = TAIL_BEGIN + current.split(TAIL_BEGIN)[1]

    parts = []
    for name, qi, an in PROCS:
        body = io.open(os.path.join(SRC, name + ".sql"), encoding="utf-8-sig").read()
        body, n = CREATE_RE.subn(
            lambda m: "%sCREATE OR ALTER%s%s" % (m.group(1), m.group(2), m.group(3)),
            body, count=1)
        if n != 1:
            sys.exit("в %s.sql не найдено CREATE PROC" % name)
        body = body.replace("\r\n", "\n").rstrip("\n")
        parts.append(
            "SET QUOTED_IDENTIFIER %s;\nSET ANSI_NULLS %s;\nGO\n%s\nGO\nPRINT '  ok: dbo.%s';\nGO\n"
            % ("ON" if qi else "OFF", "ON" if an else "OFF", body, name))

    io.open(OUT, "w", encoding="utf-8-sig", newline="\r\n").write(
        head + "\n".join(parts) + tail)
    print("пересобрано: %s (%d процедур, %d байт)"
          % (os.path.basename(OUT), len(parts), os.path.getsize(OUT)))


if __name__ == "__main__":
    main()
