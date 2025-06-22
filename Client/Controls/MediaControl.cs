using System;
using System.IO;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Classes;
using QuartzTypeLib;

namespace Merlin.Controls
{
    public interface IMediaControlContainer
    {
        Cursor Cursor { get; set; }
		bool IsPlaying { set; }
    }

    public class MediaControl
    {
        private readonly IMediaControlContainer _container;
        private IMediaControl _mediaControl;
    	private readonly Timer _timer;

        public MediaControl(IMediaControlContainer parent)
			:this()
        {
            _container = parent;
        }

    	#region Singleton

    	private MediaControl()
    	{
			_timer = new Timer();
			_timer.Tick += timer_Tick;
			_timer.Interval = 1000;
    		_mediaControl = new FilgraphManager();
    	}

    	private static MediaControl _instance;
    	private static object _locker = new object();

    	public static MediaControl Current
    	{
    		get
    		{
    			if (_instance == null)
    			{
    				lock (_locker)
    				{
    					if (_instance == null)
    					{
    						_instance = new MediaControl();
    					}
    				}
    			}

    			return _instance;
    		}
    	}

    	#endregion


		public void Play(PresentationObject roller)
        {
            try
            {
                if (roller == null) return;
                string fileName = Convert.ToString(roller[Roller.ParamNames.Path]);
                if (string.IsNullOrEmpty(fileName)) return;
            	
                StopSamplePlay(false);
                if (File.Exists(fileName))
                {
					if (_container != null)
						_container.Cursor = Cursors.WaitCursor;

                	_mediaControl = new FilgraphManager();
					_mediaControl.RenderFile(fileName);
                    _mediaControl.Run();

					_timer.Start();

					if (_container != null)
						_container.IsPlaying = true;
                }
                else
                {
                    Globals.ShowInfo("RollerFileNotFound", roller.Parameters);
                }
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
            finally
            {
				if (_container != null)
					_container.Cursor = Cursors.Default;
            }
        }

        public void Stop()
        {
			_timer.Stop();
            try
            {
                StopSamplePlay(false);
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
            finally
            {
				if (_container != null)
					_container.Cursor = Cursors.Default;
            }
        }

		private void timer_Tick(object sender, EventArgs e)
		{
			int pfs;
			_mediaControl.GetState(1000, out pfs);
			if (pfs != 2)
				Stop();
		}

        private void StopSamplePlay(bool bEnableStop)
        {
            _mediaControl.Stop();

			if (_container != null)
				_container.IsPlaying = bEnableStop;
        }

    	public bool IsPlay
    	{
			get { return _timer.Enabled; }
    	}
    }
}