using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using System;
using System.Data;

namespace Protector.Domain
{
    internal class UserDiscountMass : PresentationObject
    {
        private readonly Group _group;

        internal DataTable TableErrors { get; } = ErrorManager.CreateErrorsTable();

        public UserDiscountMass(Group group) : base(EntityManager.GetEntity((int) Entities.UserDiscountMass))
        {
            _group = group;  
        }

        public override bool Update()
        {
            foreach (DataRow row in _group.GetUsers().Rows)
            {
                UserDiscount userDiscount = new UserDiscount
                {
                    Parameters = this.Parameters
                };
                userDiscount[User.ParamNames.UserID] = row[User.ParamNames.UserID];
                userDiscount.IsNew = true;
                try 
                {
                    userDiscount.Update();
                }
                catch(Exception ex) 
                {
                    string msg = $"Ошибка при установке скидки пользователю {row[Constants.Parameters.Name]}: {MessageAccessor.GetMessage(ex.Message)} ";
                    ErrorManager.AddErrorRow(TableErrors, DateTime.Now, msg);
                }
            }

            return true;
        }
    }
}
