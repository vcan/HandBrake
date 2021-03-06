﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SummaryViewModel.cs" company="HandBrake Project (http://handbrake.fr)">
//   This file is part of the HandBrake source code - It may be used under the terms of the GNU General Public License.
// </copyright>
// <summary>
//   The Summary View Model
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HandBrakeWPF.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.ExceptionServices;
    using System.Text;
    using System.Windows.Media.Imaging;

    using HandBrake.ApplicationServices.Interop;
    using HandBrake.ApplicationServices.Interop.Model.Encoding;

    using HandBrakeWPF.EventArgs;
    using HandBrakeWPF.Factories;
    using HandBrakeWPF.Helpers;
    using HandBrakeWPF.Properties;
    using HandBrakeWPF.Services.Encode.Model;
    using HandBrakeWPF.Services.Encode.Model.Models;
    using HandBrakeWPF.Services.Interfaces;
    using HandBrakeWPF.Services.Presets.Model;
    using HandBrakeWPF.Services.Scan.Interfaces;
    using HandBrakeWPF.Services.Scan.Model;
    using HandBrakeWPF.Utilities;
    using HandBrakeWPF.ViewModels.Interfaces;

    public class SummaryViewModel : ViewModelBase, ISummaryViewModel
    {
        private readonly IScan scanService;
        private readonly IUserSettingService userSettingService;

        private Preset preset;
        private EncodeTask task;
        private Source source;
        private Title currentTitle;
        private bool isMkv;
        private int selectedPreview = 2;

        private bool isPreviousPreviewControlVisible;

        private bool isNextPreviewControlVisible;

        public SummaryViewModel(IScan scanService, IUserSettingService userSettingService)
        {
            this.scanService = scanService;
            this.userSettingService = userSettingService;
        }

        public event EventHandler<OutputFormatChangedEventArgs> OutputFormatChanged;

        public Preset Preset
        {
            get
            {
                return this.preset;
            }

            private set
            {
                if (Equals(value, this.preset)) return;
                this.preset = value;
                this.NotifyOfPropertyChange(() => this.Preset);
            }
        }

        public EncodeTask Task
        {
            get
            {
                return this.task;
            }

            set
            {
                if (Equals(value, this.task)) return;
                this.task = value;
                this.NotifyOfPropertyChange(() => this.Task);
            }
        }

        public Source Source
        {
            get
            {
                return this.source;
            }

            set
            {
                if (Equals(value, this.source)) return;
                this.source = value;
                this.NotifyOfPropertyChange(() => this.Source);
            }
        }

        public Title CurrentTitle
        {
            get
            {
                return this.currentTitle;
            }
            set
            {
                if (Equals(value, this.currentTitle)) return;
                this.currentTitle = value;
                this.NotifyOfPropertyChange(() => this.CurrentTitle);
            }
        }

        public IEnumerable<OutputFormat> OutputFormats
        {
            get
            {
                return new List<OutputFormat>
                       {
                           OutputFormat.Mp4, OutputFormat.Mkv
                       };
            }
        }

        #region DisplayProperties

        public BitmapImage PreviewImage { get; set; }
        public bool PreviewNotAvailable { get; set; }
        public int MaxWidth { get; set; }
        public int MaxHeight { get; set; }

        public string VideoTrackInfo { get; set; }
        public string AudioTrackInfo { get; set; }
        public string SubtitleTrackInfo { get; set; }
        public string ChapterInfo { get; set; }
        public string FiltersInfo { get; set; }

        public string DimensionInfo { get; set; }
        public string AspectInfo { get; set; }


        public bool IsPreviewInfoVisible { get; set; }
        public string PreviewInfo { get; set; }

        public bool IsPreviousPreviewControlVisible
        {
            get
            {
                return this.isPreviousPreviewControlVisible;
            }
            set
            {
                if (value == this.isPreviousPreviewControlVisible) return;
                this.isPreviousPreviewControlVisible = value;
                this.NotifyOfPropertyChange(() => this.IsPreviousPreviewControlVisible);
            }
        }

        public bool IsNextPreviewControlVisible
        {
            get
            {
                return this.isNextPreviewControlVisible;
            }
            set
            {
                if (value == this.isNextPreviewControlVisible) return;
                this.isNextPreviewControlVisible = value;
                this.NotifyOfPropertyChange(() => this.IsNextPreviewControlVisible);
            }
        }

        #endregion

        #region Task Properties 

        /// <summary>
        /// Gets or sets SelectedOutputFormat.
        /// </summary>
        public OutputFormat SelectedOutputFormat
        {
            get
            {
                return this.Task?.OutputFormat ?? OutputFormat.Mp4; 
            }

            set
            {
                if (!Equals(this.Task.OutputFormat, value))
                {
                    this.Task.OutputFormat = value;
                    this.Task.OutputFormat = value;
                    this.NotifyOfPropertyChange(() => this.SelectedOutputFormat);
                    this.NotifyOfPropertyChange(() => this.Task.OutputFormat);
                    this.NotifyOfPropertyChange(() => this.IsMkv);
                    this.SetExtension(string.Format(".{0}", this.Task.OutputFormat.ToString().ToLower()));

                    this.OnOutputFormatChanged(new OutputFormatChangedEventArgs(null));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether IsMkv.
        /// </summary>
        public bool IsMkv
        {
            get
            {
                return this.isMkv;
            }
            set
            {
                this.isMkv = value;
                this.NotifyOfPropertyChange(() => this.IsMkv);
            }
        }

        /// <summary>
        /// Optimise MP4 Checkbox
        /// </summary>
        public bool OptimizeMP4
        {
            get
            {
                return this.Task?.OptimizeMP4 ?? false;
            }
            set
            {
                if (value == this.Task.OptimizeMP4)
                {
                    return;
                }
                this.Task.OptimizeMP4 = value;
                this.NotifyOfPropertyChange(() => this.OptimizeMP4);
            }
        }

        /// <summary>
        /// iPod 5G Status
        /// </summary>
        public bool IPod5GSupport
        {
            get
            {
                return this.Task?.IPod5GSupport ?? false;
            }
            set
            {
                if (value == this.Task.IPod5GSupport)
                {
                    return;
                }
                this.Task.IPod5GSupport = value;
                this.NotifyOfPropertyChange(() => this.IPod5GSupport);
            }
        }

        public bool AlignAVStart
        {
            get
            {
                return this.Task?.AlignAVStart ?? false;
            }
            set
            {
                if (value == this.Task.AlignAVStart)
                {
                    return;
                }
                this.Task.AlignAVStart = value;
                this.NotifyOfPropertyChange(() => this.AlignAVStart);
            }
        }

        #endregion

        public void SetSource(Source scannedSource, Title selectedTitle, Preset currentPreset, EncodeTask encodeTask)
        {
            this.Source = scannedSource;
            this.CurrentTitle = selectedTitle;
            this.Task = encodeTask;
            this.UpdateDisplayedInfo();
        }

        public void SetPreset(Preset currentPreset, EncodeTask encodeTask)
        {
            this.Preset = currentPreset;
            this.Task = encodeTask;
            this.UpdateSettings(currentPreset);
            this.UpdateDisplayedInfo();
        }

        public void UpdateTask(EncodeTask updatedTask)
        {
            this.Task = updatedTask;
            this.UpdateDisplayedInfo();

            this.NotifyOfPropertyChange(() => this.SelectedOutputFormat);
            this.NotifyOfPropertyChange(() => this.IsMkv);

            this.NotifyOfPropertyChange(() => this.OptimizeMP4);
            this.NotifyOfPropertyChange(() => this.IPod5GSupport);
            this.NotifyOfPropertyChange(() => this.AlignAVStart);
        }

        public void UpdateDisplayedInfo()
        {
            if (this.CurrentTitle == null)
            {
                this.ClearDisplay();
                return;
            }

            this.PopulateSummaryTab();
            this.UpdatePreviewFrame();
        }

        public void SetContainer(OutputFormat container)
        {
            this.SelectedOutputFormat = container;
        }

        public void NextPreview()
        {
            int maxPreview = this.userSettingService.GetUserSetting<int>(UserSettingConstants.PreviewScanCount);
            this.selectedPreview = this.selectedPreview + 1;
            this.UpdatePreviewFrame();
            this.PreviewInfo = string.Format(ResourcesUI.SummaryView_PreviewInfo, this.selectedPreview, maxPreview);
            this.NotifyOfPropertyChange(() => this.PreviewInfo);

            if (this.selectedPreview == maxPreview)
            {
                this.IsNextPreviewControlVisible = false;
            }
        }

        public void PreviousPreview()
        {
            int maxPreview = this.userSettingService.GetUserSetting<int>(UserSettingConstants.PreviewScanCount);
            this.selectedPreview = this.selectedPreview - 1;
            this.UpdatePreviewFrame();
            this.PreviewInfo = string.Format(ResourcesUI.SummaryView_PreviewInfo, this.selectedPreview, maxPreview);
            this.NotifyOfPropertyChange(() => this.PreviewInfo);

            if (this.selectedPreview == 1)
            {
                this.IsPreviousPreviewControlVisible = false;
            }
        }

        public void SetPreviewControlVisibility(bool isPreviousVisible, bool isNextVisible)
        {
            if (this.selectedPreview > 1)
            {
                this.IsPreviousPreviewControlVisible = isPreviousVisible;
            }
            else
            {
                this.IsPreviousPreviewControlVisible = false;
            }

            if (this.selectedPreview < this.userSettingService.GetUserSetting<int>(UserSettingConstants.PreviewScanCount))
            {
                this.IsNextPreviewControlVisible = isNextVisible;
            }
            else
            {
                this.IsNextPreviewControlVisible = false;
            }
        }

        #region Private Methods

        private void UpdateSettings(Preset selectedPreset)
        {
            // Main Window Settings
            this.SelectedOutputFormat = selectedPreset.Task.OutputFormat;
            this.OptimizeMP4 = selectedPreset.Task.OptimizeMP4;
            this.IPod5GSupport = selectedPreset.Task.IPod5GSupport;
            this.AlignAVStart = selectedPreset.Task.AlignAVStart;
        }

        private void SetExtension(string newExtension)
        {
            // Make sure the output extension is set correctly based on the users preferences and selection.
            if (newExtension == ".mp4" || newExtension == ".m4v")
            {
                switch (this.userSettingService.GetUserSetting<int>(UserSettingConstants.UseM4v))
                {
                    case 0: // Auto
                        newExtension = MP4Helper.RequiresM4v(this.Task) ? ".m4v" : ".mp4";
                        break;
                    case 1: // MP4
                        newExtension = ".mp4";
                        break;
                    case 2: // M4v
                        newExtension = ".m4v";
                        break;
                }

                this.IsMkv = false;
            }

            // Now disable controls that are not required. The Following are for MP4 only!
            if (newExtension == ".mkv")
            {
                this.IsMkv = true;
                this.OptimizeMP4 = false;
                this.IPod5GSupport = false;
                this.AlignAVStart = false;
            }

            // Update The browse file extension display
            if (Path.HasExtension(newExtension))
            {
                this.OnOutputFormatChanged(new OutputFormatChangedEventArgs(newExtension));
            }

            // Update the UI Display
            this.NotifyOfPropertyChange(() => this.Task);
        }

        private void PopulateSummaryTab()
        {
            if (this.Task == null)
            {
                this.ClearDisplay();
                return;
            }

            // Dimension Section
            this.VideoTrackInfo = string.Format("{0}, {1} FPS {2}", EnumHelper<VideoEncoder>.GetDisplay(this.Task.VideoEncoder), this.Task.Framerate, this.Task.FramerateMode);
            this.NotifyOfPropertyChange(() => this.VideoTrackInfo);

            this.AudioTrackInfo = this.GetAudioDescription();
            this.NotifyOfPropertyChange(() => this.AudioTrackInfo);

            this.SubtitleTrackInfo = this.GetSubtitleDescription();
            this.NotifyOfPropertyChange(() => this.SubtitleTrackInfo);

            this.ChapterInfo = this.Task.IncludeChapterMarkers ? ResourcesUI.SummaryView_Chapters : ResourcesUI.SummaryView_NoChapters;
            this.NotifyOfPropertyChange(() => this.ChapterInfo);

            this.FiltersInfo = this.GetFilterDescription();
            this.NotifyOfPropertyChange(() => this.FiltersInfo);

            // Picutre Section
            this.DimensionInfo = string.Format("{0}x{1} {2}, {3}x{4} {5}", this.Task.Width, this.Task.Height, ResourcesUI.SummaryView_storage, this.Task.DisplayWidth, this.Task.Height, ResourcesUI.SummaryView_display);
            this.NotifyOfPropertyChange(() => this.DimensionInfo);

            this.AspectInfo = string.Empty;
            this.NotifyOfPropertyChange(() => this.AspectInfo);

            // Preview
            this.PreviewInfo = string.Format(ResourcesUI.SummaryView_PreviewInfo, this.selectedPreview, this.userSettingService.GetUserSetting<int>(UserSettingConstants.PreviewScanCount));
            this.NotifyOfPropertyChange(() => this.PreviewInfo);
        }

        private string GetFilterDescription()
        {
            if (this.Task == null)
            {
                return ResourcesUI.SummaryView_NoFilters;
            }

            List<string> filters = new List<string>();

            if (this.Task.Detelecine != Detelecine.Off)
            {
                filters.Add(ResourcesUI.SummaryView_Detelecine);
            }

            if (this.Task.DeinterlaceFilter != DeinterlaceFilter.Off)
            {
                filters.Add(EnumHelper<DeinterlaceFilter>.GetShortName(this.task.DeinterlaceFilter));
            }

            if (this.Task.Denoise != Denoise.Off)
            {
                filters.Add(this.Task.Denoise.ToString());
            }

            if (this.Task.Sharpen != Sharpen.Off)
            {
                filters.Add(this.Task.Sharpen.ToString());
            }

            if (this.Task.Deblock > 4)
            {
                filters.Add(ResourcesUI.SummaryView_Deblock);
            }

            if (this.Task.Grayscale)
            {
                filters.Add(ResourcesUI.SummaryView_Grayscale);
            }

            if (this.Task.Rotation != 0 || this.task.FlipVideo)
            {
                filters.Add(ResourcesUI.SummaryView_Rotation);
            }

            return string.Join(", ", filters).TrimEnd(',').Trim();
        }

        private string GetAudioDescription()
        {
            if (this.Task.AudioTracks.Count == 0)
            {
                return ResourcesUI.SummaryView_NoAudioTracks;
            }

            StringBuilder desc = new StringBuilder();

            if (this.Task.AudioTracks.Count >= 1)
            {
                AudioTrack track1 = this.Task.AudioTracks[0];
                HBMixdown mixdownName = HandBrakeEncoderHelpers.GetMixdown(track1.MixDown);
                string mixdown = mixdownName != null ? ", " + mixdownName.DisplayName : string.Empty;
                desc.AppendLine(string.Format("{0}{1}", EnumHelper<AudioEncoder>.GetDisplay(track1.Encoder), mixdown));
            }

            if (this.Task.AudioTracks.Count >= 2)
            {
                AudioTrack track2 = this.Task.AudioTracks[1];
                HBMixdown mixdownName = HandBrakeEncoderHelpers.GetMixdown(track2.MixDown);
                string mixdown = mixdownName != null ? ", " + mixdownName.DisplayName : string.Empty;
                desc.AppendLine(string.Format("{0}{1}", EnumHelper<AudioEncoder>.GetDisplay(track2.Encoder), mixdown));
            }

            if (this.Task.AudioTracks.Count > 2)
            {
                desc.AppendLine(string.Format("+ {0} {1}", this.Task.AudioTracks.Count - 2, ResourcesUI.SummaryView_AdditionalAudioTracks));
            }

            return desc.ToString().Trim();        
        }
        
        private string GetSubtitleDescription()
        {
            if (this.Task.AudioTracks.Count == 0)
            {
                return ResourcesUI.SummaryView_NoSubtitleTracks;
            }

            StringBuilder desc = new StringBuilder();

            if (this.Task.SubtitleTracks.Count >= 1)
            {
                SubtitleTrack track1 = this.Task.SubtitleTracks[0];
                string subtitleName = track1.IsSrtSubtitle ? track1.SrtFileName : track1.SourceTrack.ToString();
                string burned = track1.Burned ? ", " + ResourcesUI.SummaryView_Burned : string.Empty;
                desc.AppendLine(string.Format("{0}{1}", subtitleName, burned));
            }

            if (this.Task.SubtitleTracks.Count >= 2)
            {
                SubtitleTrack track2 = this.Task.SubtitleTracks[1];
                string subtitleName = track2.IsSrtSubtitle ? track2.SrtFileName : track2.SourceTrack.ToString();
                string burned = track2.Burned ? ", " + ResourcesUI.SummaryView_Burned : string.Empty;
                desc.AppendLine(string.Format("{0}{1}", subtitleName, burned));
            }

            if (this.Task.SubtitleTracks.Count > 2)
            {
                desc.AppendLine(string.Format("+ {0} {1}", this.Task.SubtitleTracks.Count - 2, ResourcesUI.SummaryView_AdditionalSubtitleTracks));
            }

            return desc.ToString().Trim();
        }

        private void ClearDisplay()
        {
            this.VideoTrackInfo = ResourcesUI.SummaryView_NoTracks;
            this.NotifyOfPropertyChange(() => this.VideoTrackInfo);

            this.AudioTrackInfo = string.Empty;
            this.NotifyOfPropertyChange(() => this.AudioTrackInfo);

            this.SubtitleTrackInfo = string.Empty;
            this.NotifyOfPropertyChange(() => this.SubtitleTrackInfo);

            this.ChapterInfo = string.Empty;
            this.NotifyOfPropertyChange(() => this.ChapterInfo);

            this.FiltersInfo = ResourcesUI.SummaryView_NoFilters;
            this.NotifyOfPropertyChange(() => this.FiltersInfo);

            this.DimensionInfo = ResourcesUI.SummaryView_NoSource;
            this.NotifyOfPropertyChange(() => this.ChapterInfo);

            this.AspectInfo = string.Empty;
            this.NotifyOfPropertyChange(() => this.FiltersInfo);
        }

        [HandleProcessCorruptedStateExceptions]
        private void UpdatePreviewFrame()
        {
            // Don't preview for small images.
            if (this.Task.Anamorphic == Anamorphic.Loose && this.Task.Width < 32)
            {
                this.PreviewNotAvailable = true;
                this.IsPreviewInfoVisible = false;
                this.NotifyOfPropertyChange(() => this.IsPreviewInfoVisible);
                return;
            }

            if ((this.Task.Anamorphic == Anamorphic.None || this.Task.Anamorphic == Anamorphic.Custom) && (this.Task.Width < 32 || this.Task.Height < 32))
            {
                this.PreviewNotAvailable = true;
                return;
            }

            BitmapImage image = null;
            try
            {
                image = this.scanService.GetPreview(this.Task, this.selectedPreview - 1, HBConfigurationFactory.Create()); 
            }
            catch (Exception exc)
            {
                this.PreviewNotAvailable = true;
                Debug.WriteLine(exc);
            }

            if (image != null)
            {
                this.PreviewNotAvailable = false;
                this.PreviewImage = image;
                this.MaxWidth = (int)image.Width;
                this.MaxHeight = (int)image.Height;
                this.IsPreviewInfoVisible = true;
                this.NotifyOfPropertyChange(() => this.IsPreviewInfoVisible);
                this.NotifyOfPropertyChange(() => this.PreviewImage);
                this.NotifyOfPropertyChange(() => this.MaxWidth);
                this.NotifyOfPropertyChange(() => this.MaxHeight);
            }
        }

        protected virtual void OnOutputFormatChanged(OutputFormatChangedEventArgs e)
        {
            this.OutputFormatChanged?.Invoke(this, e);
        }

        #endregion
    }
}
