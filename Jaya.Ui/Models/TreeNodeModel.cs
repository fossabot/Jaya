﻿using Jaya.Shared;
using Jaya.Shared.Base;
using Jaya.Shared.Models;
using Jaya.Ui.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Jaya.Ui.Models
{
    public class TreeNodeModel : ModelBase
    {
        const string COLLAPSED_IMAGE = "avares://Jaya.Ui/Assets/Images/Folder-16.png";
        const string EXPANDED_IMAGE = "avares://Jaya.Ui/Assets/Images/Folder-Open-16.png";

        TreeNodeModel _dummyChild;
        readonly SharedService _shared;

        public delegate void TreeNodeExpanded(TreeNodeModel node, bool isExpaded);
        public event TreeNodeExpanded NodeExpanded;

        public TreeNodeModel(ProviderServiceBase service, ProviderModel provider)
        {
            Service = service;
            Provider = provider;
            IconImagePath = COLLAPSED_IMAGE;
            Children = new ObservableCollection<TreeNodeModel>();

            if (Service != null && Provider != null)
            {
                _shared = ServiceLocator.Instance.GetService<SharedService>();
                _shared.ApplicationConfiguration.PropertyChanged += OnApplicationConfigChanged;
            }
        }

        ~TreeNodeModel()
        {
            if (_shared != null)
                _shared.ApplicationConfiguration.PropertyChanged -= OnApplicationConfigChanged;
        }

        #region properties

        public ProviderServiceBase Service { get; }

        public ProviderModel Provider { get; }

        public string Label
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ImagePath
        {
            get => Get<string>();
            set
            {
                Set(value);

                if (string.IsNullOrEmpty(value) || (FileSystemObject != null && FileSystemObject.Type == FileSystemObjectType.Directory && !string.IsNullOrEmpty(FileSystemObject.Path)))
                    IconImagePath = COLLAPSED_IMAGE;
                else
                    IconImagePath = ImagePath;
            }
        }

        public string IconImagePath
        {
            get => Get<string>();
            private set => Set(value);
        }

        public bool IsExpanded
        {
            get => Get<bool>();
            set
            {
                Set(value);

                if (string.IsNullOrEmpty(ImagePath))
                {
                    if (value && Children.Count > 0)
                        IconImagePath = EXPANDED_IMAGE;
                    else
                        IconImagePath = COLLAPSED_IMAGE;
                }
                else
                    IconImagePath = ImagePath;

                var handler = NodeExpanded;
                if (handler != null)
                    handler.Invoke(this, value);
            }
        }

        public bool IsHavingDummyChild => _dummyChild != null;

        public ObservableCollection<TreeNodeModel> Children { get; }

        public FileSystemObjectModel FileSystemObject
        {
            get => Get<FileSystemObjectModel>();
            set => Set(value);
        }

        private void OnApplicationConfigChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ApplicationConfigModel.IsHiddenItemVisible):
                    if (FileSystemObject != null)
                        FileSystemObject.RaisePropertyChanged(nameof(FileSystemObject.IsHidden));
                    break;
            }
        }

        #endregion

        public void AddDummyChild()
        {
            if (IsHavingDummyChild)
                return;

            var child = _dummyChild = new TreeNodeModel(Service, Provider);
            child.Label = "Loading...";
            Children.Add(child);
        }

        public void RemoveDummyChild()
        {
            if (!IsHavingDummyChild)
                return;

            Children.Remove(_dummyChild);
            _dummyChild = null;
        }
    }
}
