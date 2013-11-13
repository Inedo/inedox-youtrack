using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.YouTrack
{
    internal sealed class YouTrackIssueTrackingProviderEditor : ProviderEditorBase
    {
        private ValidatingTextBox txtBaseUrl;
        private ValidatingTextBox txtUserName;
        private PasswordTextBox txtPassword;
        private ValidatingTextBox txtReleaseField;
        private ValidatingTextBox txtMaxIssues;

        public override bool DisplayLogCommandLineArgumentsCheckBox
        {
            get { return false; }
        }

        public override void BindToForm(ProviderBase extension)
        {
            this.EnsureChildControls();

            var provider = (YouTrackIssueTrackingProvider)extension;
            this.txtBaseUrl.Text = provider.BaseUrl;
            this.txtUserName.Text = provider.UserName;
            this.txtPassword.Text = provider.Password;
            this.txtReleaseField.Text = provider.ReleaseField;
            this.txtMaxIssues.Text = provider.MaxIssues.ToString();
        }
        public override ProviderBase CreateFromForm()
        {
            this.EnsureChildControls();

            return new YouTrackIssueTrackingProvider
            {
                BaseUrl = this.txtBaseUrl.Text,
                UserName = this.txtUserName.Text,
                Password = this.txtPassword.Text,
                ReleaseField = this.txtReleaseField.Text,
                MaxIssues = int.Parse(this.txtMaxIssues.Text)
            };
        }

        protected override void CreateChildControls()
        {
            this.txtBaseUrl = new ValidatingTextBox
            {
                Width = 300,
                Required = true,
                ValidationExpression = "^[hH][tT][tT][pP][sS]?://[^/]+.*",
                DefaultText = "ex: http://test.myjetbrains.com/youtrack"
            };

            this.txtUserName = new ValidatingTextBox
            {
                Width = 300,
                DefaultText = "anonymous"
            };

            this.txtPassword = new PasswordTextBox
            {
                Width = 250
            };

            this.txtReleaseField = new ValidatingTextBox
            {
                Width = 300,
                Required = true,
                Text = "Fix versions"
            };

            this.txtMaxIssues = new ValidatingTextBox
            {
                Width = 200,
                Required = true,
                Type = ValidationDataType.Integer,
                Text = "50"
            };

            this.Controls.Add(
                new FormFieldGroup(
                    "YouTrack Address",
                    "Provide the URL of your YouTrack instance.",
                    false,
                    new StandardFormField("Address:", this.txtBaseUrl)
                ),
                new FormFieldGroup(
                    "Credentials",
                    "Specify the user name and password to use to connect to YouTrack. You may leave these blank to connect anonymously if your YouTrack instance supports guest access.",
                    false,
                    new StandardFormField("User Name:", this.txtUserName),
                    new StandardFormField("Password:", this.txtPassword)
                ),
                new FormFieldGroup(
                    "Mapping",
                    "Specify the name of the field to use in YouTrack which will contain the associated BuildMaster release number of an issue. By default, this is <i>Fix versions</i>.",
                    false,
                    new StandardFormField("Release Field:", this.txtReleaseField)
                ),
                new FormFieldGroup(
                    "Limits",
                    "Specify the maximum number of issues to request from the YouTrack instance in a request.",
                    false,
                    new StandardFormField("Maximum Issues:", this.txtMaxIssues)
                )
            );
        }
    }
}
