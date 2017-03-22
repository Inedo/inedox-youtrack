using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;
using System.Web.UI.WebControls;

namespace Inedo.BuildMasterExtensions.YouTrack.Legacy
{
    internal sealed class YouTrackIssueTrackingProviderEditor : ProviderEditorBase
    {
        private ValidatingTextBox txtBaseUrl;
        private ValidatingTextBox txtUserName;
        private PasswordTextBox txtPassword;
        private ValidatingTextBox txtReleaseField;
        private ValidatingTextBox txtMaxIssues;

        public override bool DisplayLogCommandLineArgumentsCheckBox => false;

        public override void BindToForm(ProviderBase extension)
        {
            var provider = (YouTrackIssueTrackingProvider)extension;
            this.txtBaseUrl.Text = provider.BaseUrl;
            this.txtUserName.Text = provider.UserName;
            this.txtPassword.Text = provider.Password;
            this.txtReleaseField.Text = provider.ReleaseField;
            this.txtMaxIssues.Text = provider.MaxIssues.ToString();
        }
        public override ProviderBase CreateFromForm()
        {
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
                Required = true,
                ValidationExpression = "^[hH][tT][tT][pP][sS]?://[^/]+.*",
                DefaultText = "ex: http://test.myjetbrains.com/youtrack"
            };

            this.txtUserName = new ValidatingTextBox
            {
                DefaultText = "anonymous"
            };

            this.txtPassword = new PasswordTextBox();

            this.txtReleaseField = new ValidatingTextBox
            {
                Required = true,
                Text = "Fix versions"
            };

            this.txtMaxIssues = new ValidatingTextBox
            {
                Required = true,
                Type = ValidationDataType.Integer,
                Text = "50"
            };

            this.Controls.Add(
                new SlimFormField("YouTrack URL:", this.txtBaseUrl),
                new SlimFormField("User name:", this.txtUserName),
                new SlimFormField("Password:", this.txtPassword),
                new SlimFormField("Release field:", this.txtReleaseField),
                new SlimFormField("Maximum issues:", this.txtMaxIssues)
                {
                    HelpText = "The maximum number of issues to fetch from the YouTrack instance in a request."
                }
            );
        }
    }
}
