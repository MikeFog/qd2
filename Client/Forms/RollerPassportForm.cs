using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Passport.Forms;
using Merlin.Classes;
using QuartzTypeLib;
using System;
using System.Data;
using System.IO;
using System.Windows.Forms;

namespace Merlin.Forms
{
    public partial class RollerPassportForm : PassportForm
	{
		private TimeDuration tdDuration;
		private Button btnLoad;
		private ObjectPicker2 opFirm;
        private ObjectPicker2 opAdvertType;
        private TextBox txtName;
		private CheckBox chkCommon;
		private bool isUsed = false;

		public RollerPassportForm()
		{
			InitializeComponent();			
		}

		public RollerPassportForm(PresentationObject presentationObject, DataSet ds)
			: base(presentationObject, ds)
		{
			InitializeComponent();
		}

		protected override void FormBuildCompleted()
		{
			isUsed = !pageContext.PresentationObject.IsNew && Roller.IsUsed;


			LookUp ratLookUp = (LookUp)FindControl("rolActionTypeID");
			ratLookUp.Enabled = !isUsed;

			base.FormBuildCompleted();
			btnLoad = FindControl("btnLoad") as Button;
			btnLoad.Click += OnLoadButtonClicked;
			btnLoad.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			
			tdDuration = FindControl("duration") as TimeDuration;
			tdDuration.Enabled = false;
			txtName = FindControl("name") as TextBox;
			txtName.Enabled = StringUtil.IsDBNullOrEmpty(Roller[Roller.ParamNames.Path]);

				//(!isUsed 	|| pageContext.PresentationObject.IsActionEnabled("ChangeVideoRollerName", ViewType.Journal));

			opFirm = FindControl("firmID") as ObjectPicker2;
		    opFirm.IsCreateNewAllowed = true;
			opFirm.Enabled = !Roller.IsDummy;

            opAdvertType = FindControl("AdvertTypeID") as ObjectPicker2;
			opAdvertType.ObjectSelected += new FogSoft.WinForm.ObjectDelegate(this.objectPickerAdvertType_ObjectSelected);

            chkCommon = FindControl("isCommon") as CheckBox;
			chkCommon.CheckedChanged += isCommon_CheckedChanged;
			chkCommon.Enabled = SecurityManager.LoggedUser.IsAdmin;
			firmStatusUpdate();
		}

        private void objectPickerAdvertType_ObjectSelected(PresentationObject presentationObject)
        {
			if (presentationObject.Parameters[AdvertType.ParamNames.ParentId] == DBNull.Value)
			{
				opAdvertType.ClearSelected();
				string message = Globals.GetMessage("IncorrectRolTypeSelected", null);
				FogSoft.WinForm.Forms.MessageBox.ShowExclamation(message);
			}
        }

        private void OnLoadButtonClicked(object sender, EventArgs e)
		{
			try
			{
				// Load file from the disk
				OpenFileDialog fileDialog = new OpenFileDialog();
				fileDialog.Filter =
					"Звуковые файлы|*.mp2;*.mp3;*.wav|Звуковые файлы WAV|*.wav|Звуковые файлы MP3|*.mp3|Звуковые файлы MP2|*.mp2";

				DialogResult result = fileDialog.ShowDialog(this);
				Application.DoEvents();
				if (result == DialogResult.OK)
				{
					GetFileDuration(fileDialog.FileName);
					FileInfo fi = new FileInfo(fileDialog.FileName);
					pageContext.Parameters["path"] = fi.FullName;
					txtName.Text = Path.GetFileNameWithoutExtension(fi.Name);
                    txtName.Enabled = false;

                    btnApply.Enabled = true;
				}
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void GetFileDuration(string name)
		{
			IMediaControl mediaControl = new FilgraphManager();
			mediaControl.RenderFile(name);

			int duration = (int) Decimal.Round((decimal) ((IMediaPosition)mediaControl).Duration, 0);
			tdDuration.Value = duration;
			tdDuration.Enabled = false;
		}

		private void isCommon_CheckedChanged(object sender, EventArgs e)
		{
			firmStatusUpdate();
		}

		private Roller Roller 
		{ 
			get { return (Roller)pageContext.PresentationObject; }
		}

		private void firmStatusUpdate()
		{
			if (chkCommon.Checked)
			{
				opFirm.ClearSelected();
				opAdvertType.ClearSelected();
            }

			opFirm.Enabled = !Roller.IsDummy && !chkCommon.Checked;
            opAdvertType.Enabled = !chkCommon.Checked;
		}

        protected override void ApplyChanges(Button clickedButton)
        {
			// пара Фирма+Предмет либо пустые, либо заполнены (но не для пустышек)
			if (Roller.IsDummy 
                || (opFirm.SelectedObject != null && opAdvertType.SelectedObject != null) 
				|| (opFirm.SelectedObject == null && opAdvertType.SelectedObject == null))
			{
				base.ApplyChanges(clickedButton);
			}
			else
			{
				DialogResult = DialogResult.None;
				FogSoft.WinForm.Forms.MessageBox.ShowExclamation(MessageAccessor.GetMessage("FirmAndAdvertTypeConstraint"));
			}
        }
    }
}