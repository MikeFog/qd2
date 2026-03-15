CREATE procedure [dbo].[UserDiscountIUD] 
(
    @discountID int = null out,
    @userID smallint, 
    @startDate datetime,
    @finishDate datetime,
    @maxRatio float = 1,
    @actionName varchar(16)
)
as 
begin 
    set nocount on;
    set xact_abort on;

    declare @leftFinish datetime;
    declare @rightStart datetime;

    set @startDate = dbo.ToShortDate(@startDate);
    set @finishDate = dbo.ToShortDate(@finishDate);

    set @leftFinish = DATEADD(day, -1, @startDate);
    set @rightStart = DATEADD(day, 1, @finishDate);

    if @actionName in ('AddItem','UpdateItem')
    begin 
        if @finishDate < @startDate
        begin
            raiserror('InvalidDiscountInterval', 16, 1);
            return;
        end
    end

    begin try
        begin transaction;

        if @actionName in ('AddItem','UpdateItem')
        begin
            /*
                1. Si existe un intervalo que contiene completamente al nuevo,
                   hay que partirlo en dos:
                   - la parte izquierda se queda en el registro actual
                   - la parte derecha se inserta como un nuevo registro
            */
            insert into UserDiscount
            (
                userID,
                startDate,
                finishDate,
                maxRatio
            )
            select
                ud.userID,
                @rightStart,
                ud.finishDate,
                ud.maxRatio
            from UserDiscount ud
            where ud.userID = @userID
              and (@discountID is null or ud.discountID <> @discountID)
              and ud.startDate < @startDate
              and ud.finishDate > @finishDate;

            /*
                2. Recortar por la izquierda
            */
            update ud
               set finishDate = @leftFinish
            from UserDiscount ud
            where ud.userID = @userID
              and (@discountID is null or ud.discountID <> @discountID)
              and ud.startDate < @startDate
              and ud.finishDate >= @startDate
              and ud.finishDate <= @finishDate;

            update ud
               set finishDate = @leftFinish
            from UserDiscount ud
            where ud.userID = @userID
              and (@discountID is null or ud.discountID <> @discountID)
              and ud.startDate < @startDate
              and ud.finishDate > @finishDate;

            /*
                3. Recortar por la derecha
            */
            update ud
               set startDate = @rightStart
            from UserDiscount ud
            where ud.userID = @userID
              and (@discountID is null or ud.discountID <> @discountID)
              and ud.startDate >= @startDate
              and ud.startDate <= @finishDate
              and ud.finishDate > @finishDate;

            /*
                4. Borrar intervalos totalmente cubiertos por el nuevo
            */
            delete ud
            from UserDiscount ud
            where ud.userID = @userID
              and (@discountID is null or ud.discountID <> @discountID)
              and ud.startDate >= @startDate
              and ud.finishDate <= @finishDate;

            /*
                5. Guardar el intervalo nuevo
            */
            if @actionName = 'AddItem'
            begin
                insert into UserDiscount
                (
                    userID,
                    startDate,
                    finishDate,
                    maxRatio
                )
                values
                ( 
                    @userID,
                    @startDate,
                    @finishDate,
                    @maxRatio
                );

                if @@rowcount <> 1
                begin
                    raiserror('InternalError', 16, 1);
                    rollback transaction;
                    return;
                end 

                set @discountID = SCOPE_IDENTITY();
            end
            else if @actionName = 'UpdateItem'
            begin 
                update UserDiscount
                   set startDate = @startDate,
                       finishDate = @finishDate,
                       maxRatio = @maxRatio
                 where discountID = @discountID;

                if @@rowcount <> 1
                begin
                    raiserror('InternalError', 16, 1);
                    rollback transaction;
                    return;
                end
            end
        end
        else if @actionName = 'DeleteItem'
        begin 
            delete from UserDiscount
            where discountID = @discountID;
        end

        commit transaction;
    end try
    begin catch
        if @@trancount > 0
            rollback transaction;

        throw;
    end catch
end