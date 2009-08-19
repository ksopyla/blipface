﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.4918
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BlipFace.Service.BlipFaceServices {
    using System.Runtime.Serialization;
    using System;
    
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "3.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="BlipFaceVersion", Namespace="http://schemas.datacontract.org/2004/07/BlipFace.WebServices")]
    [System.SerializableAttribute()]
    public partial class BlipFaceVersion : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged {
        
        [System.NonSerializedAttribute()]
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private string DownloadLinkField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private System.Version VersionField;
        
        [global::System.ComponentModel.BrowsableAttribute(false)]
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData {
            get {
                return this.extensionDataField;
            }
            set {
                this.extensionDataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string DownloadLink {
            get {
                return this.DownloadLinkField;
            }
            set {
                if ((object.ReferenceEquals(this.DownloadLinkField, value) != true)) {
                    this.DownloadLinkField = value;
                    this.RaisePropertyChanged("DownloadLink");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.Version Version {
            get {
                return this.VersionField;
            }
            set {
                if ((object.ReferenceEquals(this.VersionField, value) != true)) {
                    this.VersionField = value;
                    this.RaisePropertyChanged("Version");
                }
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="BlipFaceServices.IBlipFaceServices")]
    public interface IBlipFaceServices {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IBlipFaceServices/NotifyUseBlipFace", ReplyAction="http://tempuri.org/IBlipFaceServices/NotifyUseBlipFaceResponse")]
        void NotifyUseBlipFace(string guid, string version);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IBlipFaceServices/GetLatestVersion", ReplyAction="http://tempuri.org/IBlipFaceServices/GetLatestVersionResponse")]
        BlipFace.Service.BlipFaceServices.BlipFaceVersion GetLatestVersion();
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    public interface IBlipFaceServicesChannel : BlipFace.Service.BlipFaceServices.IBlipFaceServices, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    public partial class BlipFaceServicesClient : System.ServiceModel.ClientBase<BlipFace.Service.BlipFaceServices.IBlipFaceServices>, BlipFace.Service.BlipFaceServices.IBlipFaceServices {
        
        public BlipFaceServicesClient() {
        }
        
        public BlipFaceServicesClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public BlipFaceServicesClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public BlipFaceServicesClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public BlipFaceServicesClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public void NotifyUseBlipFace(string guid, string version) {
            base.Channel.NotifyUseBlipFace(guid, version);
        }
        
        public BlipFace.Service.BlipFaceServices.BlipFaceVersion GetLatestVersion() {
            return base.Channel.GetLatestVersion();
        }
    }
}
