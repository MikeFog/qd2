using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Protector.Domain;
using System.Data;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Windows.Forms.PropertyGridInternal;

namespace Protector.Forms
{
    public partial class SetUsersDiscount : Form
    {
        public SetUsersDiscount()
        {
            InitializeComponent();
        }

        override protected void OnLoad(System.EventArgs e)
        {
            base.OnLoad(e);
            try
            {
                grdUser.Entity = EntityManager.GetEntity((int)Entities.User);
                grdUser.DataSource = SecurityManager.GetUsers(true).DefaultView;
            }
            catch (System.Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
        }

        private void button2_Click(object sender, System.EventArgs e)
        {
            try
            {
                if(!ValidateDates())
                {
                    return;
                }


                foreach (var po in grdUser.Added2Checked)
                {
                    UserDiscount userDiscount = new UserDiscount();

                    userDiscount[User.ParamNames.UserID] = po[User.ParamNames.UserID];
                    userDiscount[UserDiscount.ParamNames.StartDate] = dtStartDate.Value.Date;
                    userDiscount[UserDiscount.ParamNames.FinishDate] = dtFinishDate.Value.Date;
                    userDiscount[UserDiscount.ParamNames.MaxRatio] = numRatio.Value;
                    userDiscount.IsNew = true;
                    userDiscount.Update();
                }

                MessageBox.Show(Properties.Resources.OperationSuccess, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (System.Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
        }

        private bool ValidateDates()
        {
            if (dtFinishDate.Value.Date < dtStartDate.Value.Date)
            {
                MessageBox.Show(Properties.Resources.StartDateMoreTnanFinishDate, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (grdUser.Added2Checked.Count == 0)
            {
                MessageBox.Show(Properties.Resources.SelectAtLeastOneUser, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }
    }
}
