using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using FacebookParseApp.Resources;
using System.Text;
using Facebook;
using System.Windows.Media.Imaging;
using Parse;

namespace FacebookParseApp
{
    public partial class MainPage : PhoneApplicationPage
    {
        private string _accessToken;
        private const string AppId = "190942770985124";
        private int friendCount = 0;

        /// <summary>
        /// Extended permissions is a comma separated list of permissions to ask the user.
        /// </summary>
        /// <remarks>
        /// For extensive list of available extended permissions refer to 
        /// https://developers.facebook.com/docs/reference/api/permissions/
        /// </remarks>
        private const string ExtendedPermissions = "user_about_me,read_stream,publish_stream";

        private readonly FacebookClient _fb = new FacebookClient();

        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private Uri GetFacebookLoginUrl(string appId, string extendedPermissions)
        {
            var parameters = new Dictionary<string, object>();
            parameters["client_id"] = appId;
            parameters["redirect_uri"] = "https://www.facebook.com/connect/login_success.html";
            parameters["response_type"] = "token";
            parameters["display"] = "touch";

            // add the 'scope' only if we have extendedPermissions.
            if (!string.IsNullOrEmpty(extendedPermissions))
            {
                // A comma-delimited list of permissions
                parameters["scope"] = extendedPermissions;
            }

            return _fb.GetLoginUrl(parameters);
        }

        void _webBrowser_Navigated(object sender, NavigationEventArgs e)
        {
            FacebookOAuthResult result;

            if (!_fb.TryParseOAuthCallbackUrl(e.Uri, out result))
            {
                return;
            }

            if (result.IsSuccess)
            {
                _accessToken = result.AccessToken;
                GetFriendsList(_accessToken);
            }

            else
            {
                MessageBox.Show(result.ErrorDescription);
            }

        }

        private void GetFriendsList(string accesstoken)
        {
            var fb = new FacebookClient(accesstoken);
            List<string> names = new List<string>();
            List<string> ids = new List<string>();
            List<BitmapImage> photos = new List<BitmapImage>();

            fb.GetCompleted += (o, e) =>
            {
                if (e.Error != null)
                {
                    Dispatcher.BeginInvoke(() => MessageBox.Show(e.Error.Message));
                    return;
                }

                var result = (IDictionary<string, object>)e.GetResultData();
                dynamic data = (IList<object>)result["data"];
                foreach (dynamic item in data)
                {
                    names.Add(item["name"]);
                    ids.Add(item["id"]);
                }

                //foreach (string id in ids)
                //{
                //    string profilePictureUrl = string.Format("https://graph.facebook.com/{0}/picture?type={1}", id, "large");

                //    photos.Add(new BitmapImage(new Uri(profilePictureUrl)));
                //}

                for (int i = 0; i < names.Count; ++i)
                {
                    PopulateParse(names[i], ids[i]);
                }

                List<string> temp = names;
            };

            fb.GetAsync("me/friends");

        }

        private async void PopulateParse(string name, string id)
        {
            var newField = new ParseObject("FacebookFriends");
            newField["SerialNo"] = ++friendCount;
            newField["Name"] = name;
            newField["Id"] = id;
            await newField.SaveAsync();
        }

        private async void DeleteParse(string objectid)
        {
            ParseQuery<ParseObject> query = ParseObject.GetQuery("FacebookFriends");
            ParseObject gameScore = await query.GetAsync(objectid);
            await gameScore.DeleteAsync();
        }

        private void _webBrowser_Loaded(object sender, RoutedEventArgs e)
        {
            _loginButton.Visibility = System.Windows.Visibility.Visible;
            _webBrowser.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _webBrowser.Visibility = System.Windows.Visibility.Visible;
            var loginUrl = GetFacebookLoginUrl(AppId, ExtendedPermissions);
            _webBrowser.Navigate(loginUrl);
        }
    }
}