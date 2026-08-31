/*
    A/B: старая и новая версии GetIssuesPrice / SetIssueRatio из ОДИНАКОВОГО
    исходного состояния (ветка feature/recalc-join-fix).

    Это и есть доказательство эквивалентности правки. Идея: каждая акция
    пересчитывается дважды - старой и новой реализацией, - и оба раза с
    отката, то есть обе версии стартуют с одного и того же состояния БД.

    ПОДГОТОВКА (выполняется один раз, на восстановленной копии прода):

      1. Создать копии СТАРЫХ версий под именами *_old:

           git show master:"ArtvisDB/dbo/Stored Procedures/GetIssuesPrice.sql"
           git show master:"ArtvisDB/dbo/Stored Procedures/SetIssueRatio.sql"

         В каждой заменить имя процедуры на GetIssuesPrice_old / SetIssueRatio_old
         и CREATE на CREATE OR ALTER, затем накатить.

      2. Создать ActionRecalculate_old - копию ActionRecalculate, в которой
         вызовы GetIssuesPrice / SetIssueRatio заменены на *_old.

      3. Накатить НОВЫЕ GetIssuesPrice / SetIssueRatio (из этой ветки).

      4. Выполнить этот скрипт. Ожидаемый результат - «расхождений old vs new: 0».

      5. Убрать копии:
           DROP PROCEDURE dbo.ActionRecalculate_old, dbo.GetIssuesPrice_old, dbo.SetIssueRatio_old;

    Выборка стратифицированная: по 25 акций на каждый campaignTypeID плюс
    25 акций с кампаниями, пересекающими границу текущего месяца (только они
    проходят через GetPriceByPeriod и реально переставляют ratio в фазе 3).

    Результат прогона на восстановленном проде 30.08.2026:
        акций 121, кампаний сравнено 336, расхождений 0, ошибок 0.
*/
SET NOCOUNT ON;
DECLARE @theDate DATETIME = CONVERT(DATETIME, CONVERT(VARCHAR(6), GETDATE(), 112) + '01', 112);
DECLARE @acts TABLE(actionID INT PRIMARY KEY);
-- та же стратифицированная выборка, что в check-скрипте
INSERT INTO @acts SELECT x.actionID FROM (
  SELECT c.actionID, rn=ROW_NUMBER() OVER(PARTITION BY c.campaignTypeID ORDER BY MAX(a.modDate) DESC)
  FROM Campaign c JOIN [Action] a ON a.actionID=c.actionID GROUP BY c.actionID,c.campaignTypeID) x
WHERE x.rn<=25 GROUP BY x.actionID;
INSERT INTO @acts SELECT x.actionID FROM (
  SELECT c.actionID, rn=ROW_NUMBER() OVER(ORDER BY MAX(a.modDate) DESC)
  FROM Campaign c JOIN [Action] a ON a.actionID=c.actionID
  WHERE c.startDate<@theDate AND c.finishDate>=@theDate GROUP BY c.actionID) x
WHERE x.rn<=25 AND NOT EXISTS(SELECT 1 FROM @acts z WHERE z.actionID=x.actionID) GROUP BY x.actionID;

DECLARE @s TABLE(k CHAR(3), actionID INT, campaignID INT, tariffPrice DECIMAL(18,2), discount DECIMAL(9,4),
  issuesCount INT, issuesDuration INT, programsCount INT, timeBonus INT, sd DATETIME, fd DATETIME,
  managerDiscount DECIMAL(18,10), finalPrice DECIMAL(18,2), ratioSum DECIMAL(38,10), totalPrice DECIMAL(18,2));
DECLARE @a INT, @tp DECIMAL(18,2), @err INT=0;
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT actionID FROM @acts;
OPEN cur; FETCH NEXT FROM cur INTO @a;
WHILE @@FETCH_STATUS=0
BEGIN
  BEGIN TRY
    BEGIN TRAN;
    EXEC dbo.ActionRecalculate_old @actionID=@a, @loggedUserID=NULL, @totalPrice=@tp OUT;
    INSERT INTO @s SELECT 'old', c.actionID, c.campaignID, c.tariffPrice, c.discount, c.issuesCount,
      c.issuesDuration, c.programsCount, c.timeBonus, c.startDate, c.finishDate, c.managerDiscount, c.finalPrice,
      (SELECT ISNULL(SUM(i.ratio),0) FROM Issue i WHERE i.campaignID=c.campaignID),
      (SELECT a2.totalPrice FROM [Action] a2 WHERE a2.actionID=@a)
    FROM Campaign c WHERE c.actionID=@a;
    ROLLBACK;
    BEGIN TRAN;
    EXEC dbo.ActionRecalculate @actionID=@a, @loggedUserID=NULL, @totalPrice=@tp OUT;
    INSERT INTO @s SELECT 'new', c.actionID, c.campaignID, c.tariffPrice, c.discount, c.issuesCount,
      c.issuesDuration, c.programsCount, c.timeBonus, c.startDate, c.finishDate, c.managerDiscount, c.finalPrice,
      (SELECT ISNULL(SUM(i.ratio),0) FROM Issue i WHERE i.campaignID=c.campaignID),
      (SELECT a2.totalPrice FROM [Action] a2 WHERE a2.actionID=@a)
    FROM Campaign c WHERE c.actionID=@a;
    ROLLBACK;
  END TRY
  BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; SET @err+=1; END CATCH
  FETCH NEXT FROM cur INTO @a;
END
CLOSE cur; DEALLOCATE cur;
SELECT [акций]=(SELECT COUNT(*) FROM @acts), [кампаний сравнено]=(SELECT COUNT(*) FROM @s WHERE k='old'), [ошибок]=@err;
SELECT [расхождений old vs new] = (SELECT COUNT(*) FROM (
   SELECT actionID,campaignID,tariffPrice,discount,issuesCount,issuesDuration,programsCount,timeBonus,sd,fd,managerDiscount,finalPrice,ratioSum,totalPrice FROM @s WHERE k='old'
   EXCEPT
   SELECT actionID,campaignID,tariffPrice,discount,issuesCount,issuesDuration,programsCount,timeBonus,sd,fd,managerDiscount,finalPrice,ratioSum,totalPrice FROM @s WHERE k='new') d);
