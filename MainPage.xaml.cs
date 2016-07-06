using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 を参照してください

namespace Lagopus_UWP
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
        private Dictionary<string, OAuth> AccountDictionary = new Dictionary<string, OAuth>();

        //アカウントを追加するのボタンが押されたら処理を始める
        private async void Add_NewAcount(object sender, TappedRoutedEventArgs e)
        {
            OAuth oauth = new OAuth();
            await oauth.GetRequesttoken();
            this.Frame.Navigate(typeof(PinPage), oauth.PostString);
        }
        //PinPageから戻ってきたら処理
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            //戻ってきたらいろいろと更新
            //アカウントの更新
            string[] newcomer = e.Parameter as string[];
            if (newcomer != null)
            {
                if (newcomer[3] != "")
                {
                    OAuth NewAccount = new OAuth();
                    NewAccount.RegisterAccessKey(newcomer);
                    
                    var user = new User();
                    //Users.GetIconNormal(user);
                    //NewAccount.icon = user.UserIcon_normal;
                    //NewAccount.UserName = user.name;
                    user.screen_name = newcomer[2];
                    Add_NewAcountList(user);
                    Add_ComboboxAccount(user);
                    AccountDictionary.Add(user.screen_name, NewAccount);
                    await Shome_timeline(NewAccount);
                }
            }
        }


        //アカウントリストにアカウントを追加します
        private void Add_NewAcountList(User user)
        {
            StackPanel UserPanel = new StackPanel();
            UserPanel.Orientation = Orientation.Horizontal;

            TextBlock Username = new TextBlock();
            Username.Text = "@" + user.screen_name;
            Username.FontSize = 18;

            Image icon = new Image();
            //icon.Source = user.UserIcon_normal.Source;
            UserPanel.Name = user.screen_name;
            //UserPanel.Children.Add(icon);
            UserPanel.Children.Add(Username);

            //右クリックの操作を導入します
            AccountList.Items.Add(UserPanel);

        }

        //コンボボックスにアカウントを追加します
        private void Add_ComboboxAccount(User user)
        {
            ComboBoxItem newAccount1 = new ComboBoxItem();
            newAccount1.Content = String.Format("@{0}", user.screen_name);
            newAccount1.Tag = user.screen_name;

            ComboBoxItem newAccount2 = new ComboBoxItem();
            newAccount2.Content = String.Format("@{0}", user.screen_name);
            newAccount2.Tag = user.screen_name;

            TL1Account.Items.Add(newAccount1);
        }

        //TLにツイートを追加します
        private void Add_TimeLineListView(Tweet tweet, OAuth user)
        {
            //いろんなところで使うidを確保
            string id = tweet.id_str;

            //Windows appsにはDockPanelがないのでStackPanelでがんばる

            StackPanel Top = new StackPanel();
            Top.Orientation = Orientation.Horizontal;

            if (tweet.user.UserIcon_normal == null)
                tweet.user = Users.GetIconNormal(tweet.user);
            Image icon = new Image();
            icon.Source = tweet.user.UserIcon_normal.Source;

            StackPanel userprofile = new StackPanel();
            TextBlock user_name = new TextBlock();

            user_name.Text = tweet.user.name;
            user_name.Foreground = new SolidColorBrush(Colors.White);
            user_name.FontSize = 18;

            TextBlock screen_name = new TextBlock();
            screen_name.Text = "@" + tweet.user.screen_name;
            screen_name.Foreground = new SolidColorBrush(Colors.LightGray);
            screen_name.FontSize = 14;

            userprofile.Children.Add(user_name);
            userprofile.Children.Add(screen_name);

            Top.Children.Add(icon);
            Top.Children.Add(userprofile);
            Top.Orientation = Orientation.Horizontal;

            TextBlock text = new TextBlock();
            text.Text = tweet.text;
            text.TextWrapping = TextWrapping.Wrap;
            text.Foreground = new SolidColorBrush(Colors.White);

            //形態素解析させる
            //形態素解析を行い結果を記録（工事中）

            StackPanel Bottom = new StackPanel();
            Bottom.Orientation = Orientation.Horizontal;

            if (tweet.user.id_str == user.UserId || tweet.user.id == decimal.Parse(user.UserId))
            {
                //Delete系の作業を行います
                TextBlock Delete = new TextBlock();
                Delete.Text = "🚮";
                Delete.FontSize = 14;
                Delete.Foreground = new SolidColorBrush(Colors.White);
                Delete.Tapped += async (object senders, TappedRoutedEventArgs es) =>
                {
                    Tweet result = await Statuses.destroy(user, tweet.id_str);
                    if (result.id_str != null)
                    {
                        Delete.Text = "This Tweet have been deleted";
                    }
                };
                Bottom.Children.Add(Delete);

            }
            else
            {
                TextBlock Retweet = new TextBlock();
                Retweet.Text = "RT";
                Retweet.FontSize = 14;

                if (tweet.retweeted == true)
                    Retweet.Foreground = new SolidColorBrush(Colors.YellowGreen);
                else
                    Retweet.Foreground = new SolidColorBrush(Colors.White);

                //イベントハンドラを追加
                Retweet.Tapped += async (object senders, TappedRoutedEventArgs es) =>
                {
                    if (tweet.retweeted)
                    {
                        //ここがいろいろ面倒くさいところ
                        SortedDictionary<string, string> postdata = new SortedDictionary<string, string>
                        {
                            {"id",tweet.id_str},
                            {"include_my_retweet","true"}
                        };
                        Tweet retweeted = await Statuses.show(user, postdata);
                        Tweet result = await Statuses.destroy(user, retweeted.id_str);
                        if (result.id_str != null)
                        {
                            Retweet.Foreground = new SolidColorBrush(Colors.White);
                        }
                    }
                    else
                    {
                        Tweet result = await Statuses.retweet(user, tweet.id_str);
                        if (result.id_str != null)
                        {
                            Retweet.Foreground = new SolidColorBrush(Colors.YellowGreen);
                        }
                    }
                };
                Bottom.Children.Add(Retweet);
            }

            TextBlock Favorite = new TextBlock();

            if (tweet.favorited == true)
                Favorite.Foreground = new SolidColorBrush(Colors.Yellow);
            else
                Favorite.Foreground = new SolidColorBrush(Colors.White);


            Favorite.Text = "★";
            Favorite.FontSize = 14;

            Favorite.Tapped += async (object senders, TappedRoutedEventArgs es) =>
            {
                if (tweet.favorited)
                {
                    Tweet result = await Favorites.destroy(user, tweet.id_str);
                    if (result.id_str != null)
                    {
                        Favorite.Foreground = new SolidColorBrush(Colors.White);
                    }
                }
                else
                {
                    Tweet result = await Favorites.create(user, tweet.id_str);
                    if (result.id_str != null)
                    {
                        Favorite.Foreground = new SolidColorBrush(Colors.Yellow);
                    }
                }
            };

            Bottom.Children.Add(Favorite);

            StackPanel tweetpanel = new StackPanel();
            tweetpanel.Children.Add(Top);
            tweetpanel.Children.Add(text);
            tweetpanel.Children.Add(Bottom);

            ListView1.Items.Insert(0, tweetpanel);
        }

        //画面サイズが変わるたび呼び出されます
        private void pageRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            LandScapeResize();
        }
        private void LandScapeResize()
        {
            //画面サイズに合わせてタイムラインのサイズを決めていきます
            ListView1.Width = (pageRoot.ActualWidth - 340) / 2;
            ListView1.Height = pageRoot.ActualHeight - 124;
            AccountList.Height = pageRoot.ActualHeight - 403;
        }

        //ツイートボックスのテキストが変更された際文字数をカウントします
        private void TweetString_TextChanged(object sender, TextChangedEventArgs e)
        {
            int remain = 140 - TweetString.Text.Length;
            RemainCharacter.Text = String.Format("{0}", remain);
        }

        //ツイートボタンの処理
        private async void TweetButton_Click(object sender, RoutedEventArgs e)
        {

            List<StackPanel> Check_Acount = new List<StackPanel>();
            if (AccountList.SelectedItems.Count > 0)
            {
                for (int i = 0; i < AccountList.SelectedItems.Count; i++)
                {
                    Check_Acount.Add((StackPanel)AccountList.SelectedItems[i]);
                    await Statuses.update(AccountDictionary[Check_Acount[i].Name], TweetString.Text);
                }
                TweetString.Text = string.Empty;

            }
        }

        //複数回ツイートします
        private async void TweetButton2_Click(object sender, RoutedEventArgs e)
        {
            int times = int.Parse(Times.Text);
            List<StackPanel> Check_Acount = new List<StackPanel>();
            if (AccountList.SelectedItems.Count > 0)
            {
                for (int i = 0; i < AccountList.SelectedItems.Count; i++)
                {
                    for (int j = 0; j < times; j++)
                    {
                        Check_Acount.Add((StackPanel)AccountList.SelectedItems[i]);
                        await Statuses.update(AccountDictionary[Check_Acount[i].Name], TweetString.Text + "　(" + j + ")");
                    }
                }
                TweetString.Text = string.Empty;
            }
        }
        //回数のところには数字しか入れられないようにします
        private void Times_TextChanged(object sender, TextChangedEventArgs e)
        {
            foreach (char character in Times.Text)
            {
                if (character < '0' || '9' < character)
                    Times.Text = "";
            }
        }


        //コンボボックス内の対象が変わると呼び出されツイートの更新を自動で行います
        private void ReadTLChange(object sender, SelectionChangedEventArgs e)
        {
            ListView1.Items.Clear();
            switch (Convert.ToString(ReadTL.SelectedItem))
            {
                case "TimeLine":
                    break;
                case "Mension":
                    break;
                case "UserTimeLine":
                    break;
                case "Favorite":
                    break;
            }

        }

        //リストボックス1用の更新ボタンです
        private void Refresh1_Click(object sender, RoutedEventArgs e)
        {
            //いったんリフレッシュする
           ListView1.Items.Clear();
        }

        //ストリーミングを続けるかどうか
        private bool IsStreaming = true;
        //ストリーミングホームライムラインの取得
        private async Task Shome_timeline(OAuth oauth)
        {
            string APIURL = "https://userstream.twitter.com/1.1/user.json";
            SortedDictionary<string, string> postdata = new SortedDictionary<string, string>();
            var res = await oauth.StreamGet(oauth, APIURL, postdata);
            using (var sr = new StreamReader(await res.Content.ReadAsStreamAsync()))
            {
                while (IsStreaming)
                {
                    // 何か処理をする
                    string newer = await sr.ReadLineAsync();
                    if (newer != "")
                    {
                        Streamingaction(newer, oauth);
                    }
                }
            }

        }

        //にゃーんとつぶやいた回数を記録します
        private Dictionary<string, int> Nyan = new Dictionary<string, int>();

        //ストリーミングで流れてきたやつを加工します（にゃーん処理あり）
        private async void Streamingaction(string result, OAuth oauth)
        {
            //正規表現を用いて何が流れてきたのかを解析する
            string type = Regex.Match(result, "{\"(.*?)\":(.*?)").Groups[1].Value;
            switch (type)
            {
                case "delete":
                    //ツイートが消去されたときに来る
                    Tweet deleted = Serialize.JsontoTweet(result);
                    //今のところ特に処理する予定はない
                    break;
                case "created_at":
                    //誰かがツイートされたときに来るそのまま処理
                    Tweet created = Serialize.JsontoTweet(result);

                    //本線に戻して処理してあげる
                    //await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    //() => { Add_TimeLineListView(created, oauth); });
                    if(created.text=="にゃーん")
                    {
                        List<StackPanel> Check_Acount = new List<StackPanel>();
                        if (AccountList.SelectedItems.Count > 0)
                        {
                            for (int i = 0; i < AccountList.SelectedItems.Count; i++)
                            {
                                Check_Acount.Add((StackPanel)AccountList.SelectedItems[i]);
                                int r;
                                string reply = created.user.screen_name;
                                reply += String.Format("さんへ　何がにゃーんですか、恥を知りなさい。");
                                if (Nyan.TryGetValue(created.user.screen_name, out r))
                                {
                                    Nyan[created.user.screen_name]++;
                                }
                                else
                                {
                                    Nyan.Add(created.user.screen_name, 1);
                                }
                                reply += String.Format("（{0}回目）　#にゃーん", Nyan[created.user.screen_name]);

                                await Statuses.update(AccountDictionary[Check_Acount[i].Name], reply);
                            }

                        }
                    }
                    break;
                case "friends":
                    //ストリームに接続すると流れてくる
                    //ローカルに保存したxmlファイルを読みだして比較（工事中）
                    break;
                case "event":
                    //ツイート以外の様々なイベントで出てくる
                    string event_type = Regex.Match(result, "{\"event\":\"(.*?)\",(.*?)}").Groups[1].Value;
                    string content = "{" + Regex.Match(result, "{\"event\":\"(.*?)\",(.*?)}").Groups[2].Value;

                    switch (event_type)
                    {
                        //工事中…
                    }
                    break;
            }

        }

    }

    //APIどおり
    public static class Statuses
    {
        //一般的なツイートの用法
        public async static Task<Tweet> update(OAuth oauth, string status)
        {
            status = WebUtility.UrlEncode(status);
            string APIURL = "https://api.twitter.com/1.1/statuses/update.json";
            SortedDictionary<string, string> postdata = new SortedDictionary<string, string>
            {
                {"status",status}
            };
            string resultstr = await oauth.RestPOST(oauth, APIURL, postdata);
            //Tweet result = Serialize.JsontoOwnTweet(resultstr, user);
            var result = new Tweet();
            return result;
        }

        //リツイート
        public async static Task<Tweet> retweet(OAuth oauth, string id)
        {
            string APIURL = String.Format("https://api.twitter.com/1.1/statuses/retweet/{0}.json", id);
            SortedDictionary<string, string> postdata = new SortedDictionary<string, string>();
            string resultstr = await oauth.RestPOST(oauth, APIURL, postdata);
            Tweet result = Serialize.JsontoTweet(resultstr);
            return result;
        }

        //ツイートの詳細を閲覧します
        public async static Task<Tweet> show(OAuth oauth, string id)
        {
            string APIURL = "https://api.twitter.com/1.1/statuses/show.json";
            SortedDictionary<string, string> postdata = new SortedDictionary<string, string>
            {
                {"id",id}
            };
            string resultstr = await oauth.RestGET(oauth, APIURL, postdata);
            Tweet result = Serialize.JsontoTweet(resultstr);
            return result;
        }
        public async static Task<Tweet> show(OAuth oauth, SortedDictionary<string, string> postdata)
        {
            string APIURL = "https://api.twitter.com/1.1/statuses/show.json";
            string resultstr = await oauth.RestGET(oauth, APIURL, postdata);
            Tweet result = Serialize.JsontoTweet(resultstr);
            return result;
        }

        //ツイートを消去します
        public async static Task<Tweet> destroy(OAuth oauth, string id)
        {
            string APIURL = "https://api.twitter.com/1.1/statuses/destroy/" + id + ".json";
            SortedDictionary<string, string> postdata = new SortedDictionary<string, string>();
            string resultstr = await oauth.RestPOST(oauth, APIURL, postdata);
            Tweet result = Serialize.JsontoTweet(resultstr);
            return result;
        }

        //レストホームタイムラインの取得
        public async static Task<List<Tweet>> Rhome_timeline(OAuth oauth)
        {
            string APIURL = "https://api.twitter.com/1.1/statuses/home_timeline.json";
            SortedDictionary<string, string> postdata = new SortedDictionary<string, string>();
            string resultstr = await oauth.RestGET(oauth, APIURL, postdata);
            List<Tweet> result = Serialize.JsontoTweets(resultstr, oauth);
            return result;
        }


    }
    public static class Search
    { }
    public static class Streaming
    {


    }
    public static class DirectMessages
    { }
    public static class Friends_Followers
    { }
    public static class Users
    {
        //画像データは通信速度の向上のため載せずに値を返します
        public async static Task<User> show(OAuth oauth,User user)
        {
            string APIURL = "https://api.twitter.com/1.1/users/show.json";

            SortedDictionary<string, string> postdata = new SortedDictionary<string, string>
                {
                    {"user_id",user.id_str}
                };
            string result = await oauth.RestGET(oauth, APIURL, postdata);
            User u = Serialize.JsontoUser(result,true);
            return u;
        }

        //画像ファイルを読み込みます
        public static User GetIconNormal(User user)
        {
            if (user.profile_image_url_https != null)
            {
                BitmapImage bitmapicon = new BitmapImage(new Uri(user.profile_image_url_https));
                user.UserIcon_normal = new Image();
                user.UserIcon_normal.Source = bitmapicon;
            }
            return user;
        }

    }
    public static class SuggestedUsers
    { }
    public static class Favorites
    {
        //ふぁぼります
        public static async Task<Tweet> create(OAuth oauth, string id)
        {
            string APIURL = "https://api.twitter.com/1.1/favorites/create.json";
            SortedDictionary<string, string> postdata = new SortedDictionary<string, string>
            {
                {"id",id}
            };
            string resultstr = await oauth.RestPOST(oauth, APIURL, postdata);
            Tweet result = Serialize.JsontoTweet(resultstr);
            return result;
        }

        //あんふぁぼります
        public static async Task<Tweet> destroy(OAuth oauth, string id)
        {
            string APIURL = "https://api.twitter.com/1.1/favorites/destroy.json";
            SortedDictionary<string, string> postdata = new SortedDictionary<string, string>
            {
                {"id",id}
            };
            string resultstr = await oauth.RestPOST(oauth, APIURL, postdata);
            Tweet result = Serialize.JsontoTweet(resultstr);
            return result;
        }
    }
    public static class Lists
    { }

    //変換系
    public static class Serialize
    {
        //Tweetへの変換
        public static Tweet JsontoTweet(string result)
        {
            Tweet tweet = new Tweet();
            DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(Tweet));

            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(result));
            tweet = (Tweet)dcjs.ReadObject(stream);
            User tweeteduser = new User();
            string userstring = Regex.Match(result, "(.*?)user\":{(.*?)}(.*?)").Groups[2].Value;
            if (userstring != "")
            {
                userstring = "{" + userstring + "}";
                tweeteduser = JsontoUser(userstring,false);

                //自分とユーザー名が一致している場合、画像ファイルの場所が指定されないのでここで画像を追加しておきます
                //if (tweeteduser.profile_image_url_https == null)
                //{
                //    tweeteduser.UserIcon_normal = user.icon;
                //    tweeteduser.screen_name = user.ScreenName;
                //    tweeteduser.name = user.UserName;

                //}

                tweet.user = tweeteduser;

                return tweet;
            }
            else
                return null;
        }
        public static List<Tweet> JsontoTweets(string result, OAuth user)
        {
            List<Tweet> tweet = new List<Tweet>();


            //処理をしやすくするために最初の文字を消去
            result = result.Remove(0, 1);
            //処理をしやすくするために後ろに適当な文字を入れる
            result += "]";

            StringReader jsonstring = new StringReader(result);
            List<string> tweetjson = new List<string>();

            //正規表現を用いて },{でテキストを切っていきます
            if (result.Length > 2)
            {
                //初期化
                string[] comparison = new string[3];
                comparison[0] = null;
                comparison[1] = null;
                comparison[2] = null;

                string text = null;
                while (jsonstring.Peek() > -1)
                {
                    comparison[2] = comparison[1];
                    comparison[1] = comparison[0];
                    comparison[0] = Convert.ToString(Convert.ToChar(jsonstring.Read()));
                    text += comparison[2];
                    if (String.Format("{0}{1}{2}", comparison[2], comparison[1], comparison[0]) == "},{")
                    {
                        tweetjson.Add(text);
                        comparison[1] = null;
                        //リセット
                        text = null;
                    }
                }
            }

            DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(Tweet));

            //ツイート1つ1つの処理はこちらから
            for (int i = 0; i < tweetjson.Count; i++)
            {
                //jsonデータを使える形式に成型します
                string text = null;
                text += tweetjson[i];
                text += null;

                MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
                Tweet a_tweet = (Tweet)dcjs.ReadObject(stream);

                //次にシリアライザーで自動的に変換されないUserとかのやつを実装
                User tweeteduser = new User();
                string userstring = Regex.Match(result, "(.*?)user\":(.*?),\"(.*?)").Groups[2].Value;
                userstring += "}";
                tweeteduser = JsontoUser(userstring,false);
                if (tweeteduser.profile_image_url_https == null)
                {
                    tweeteduser.UserIcon_normal = user.icon;
                    tweeteduser.name = user.UserName;
                }

                a_tweet.user = tweeteduser;
                tweet.Add(a_tweet);
            }

            return tweet;
        }

        //Twitterから送られてきたJsonデータをUser型に変換
        //flagにはTweetの読み取りを行うかどうか指定
        public static User JsontoUser(string result,bool flag)
        {
            User user = new User();

            DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(User));
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(result));
            stream.Position = 0;
            user = (User)dcjs.ReadObject(stream);

            //プロテクトだけはそのまま読めないので正規表現で読み取ってあげる
            string beta = Regex.Match(result, "(.*?)protected\":(.*?),\"(.*?)").Groups[2].Value;
            if (beta == "true" || beta == "false")
            {
                user.Protected = bool.Parse(beta);
            }
            else if (beta == null)
            {
                user.Protected = false;
            }

            return user;
            }
            
    }


    //Object関係
    [DataContract]
    public class Tweet
    {
        [DataMember]
        public string created_at { get; set; }

        public Tweet current_user_retweet;

        public Entity entities;

        [DataMember]
        public int favorite_count { get; set; }
        [DataMember]
        public bool favorited { get; set; }
        [DataMember]
        public string filter_level { get; set; }

        [DataMember]
        public decimal id { get; set; }
        [DataMember]
        public string id_str { get; set; }

        [DataMember]
        public string in_reply_to_screen_name { get; set; }
        //[DataMember]
        public decimal in_reply_to_status_id { get; set; }
        [DataMember]
        public string in_reply_to_status_id_str { get; set; }
        //[DataMember]
        public decimal in_reply_to_user_id { get; set; }
        [DataMember]
        public string in_reply_to_user_id_str { get; set; }

        [DataMember]
        public string lang { get; set; }

        public Place place;

        [DataMember]
        public bool possibly_sensitive { get; set; }

        [DataMember]
        public bool retweeted { get; set; }

        [DataMember]
        public int retweet_count { get; set; }

        public Tweet retweeted_status;

        [DataMember]
        public string source { get; set; }

        [DataMember]
        public string text { get; set; }
        [DataMember]
        public bool truncated { get; set; }

        public User user;

        [DataMember]
        public bool withheld_copyright { get; set; }
        [DataMember]
        public string[] withheld_in_countries { get; set; }
        [DataMember]
        public string withheld_scope { get; set; }

    }
    [DataContract]
    public class User
    {
        //[DataMember]
        public string contributors_enabled { get; set; }
        [DataMember]
        public string created_at { get; set; }

        //[DataMember]
        public bool default_profile { get; set; }
        //[DataMember]
        public bool default_profile_image { get; set; }

        //[DataMember]
        public string description { get; set; }

        //[DataMember]
        public Entity entities;

        [DataMember]
        public int favourites_count { get; set; }

        //DataMember]
        public bool follow_request_sent { get; set; }
        //[DataMember]
        public bool following { get; set; }

        //[DataMember]
        public int followers_count { get; set; }
        [DataMember]
        public int friends_count { get; set; }
        //[DataMember]
        public bool geo_enabled { get; set; }

        [DataMember]
        public decimal id { get; set; }
        [DataMember]
        public string id_str { get; set; }

        //bigger=個人ページ用,normal=TL表示用,mini=ふぁぼ表示用
        public Image UserIcon_bigger;
        public Image UserIcon_normal;
        public Image UserIcon_mini;

        public Image Bannerweb;

        //[DataMember]
        public bool is_translator { get; set; }
        //[DataMember]
        public string lang { get; set; }
        //[DataMember]
        public int listed_count { get; set; }
        //[DataMember]
        public string location { get; set; }
        [DataMember]
        public string name { get; set; }

        //[DataMember]
        public string profile_background_color { get; set; }
        //[DataMember]
        public string profile_background_image_url { get; set; }
        //[DataMember]
        public string profile_background_image_url_https { get; set; }
        //[DataMember]
        public bool profile_background_tile { get; set; }
        //[DataMember]
        public string profile_banner_url { get; set; }

        [DataMember]
        public string profile_image_url { get; set; }
        [DataMember]
        public string profile_image_url_https { get; set; }
        [DataMember]
        public string profile_icon_url { get; set; }

        //[DataMember]
        public string profile_link_color { get; set; }
        //[DataMember]
        public string profile_sidebar_border_color { get; set; }
        //[DataMember]
        public string profile_sidebar_fill_color { get; set; }
        //[DataMember]
        public string profile_text_color { get; set; }
        //[DataMember]
        public bool profile_use_background_image { get; set; }

        public bool Protected { get; set; }

        [DataMember]
        public string screen_name { get; set; }

        //[DataMember]
        public bool show_all_inline_media { get; set; }

        public Tweet status;

        //[DataMember]
        public int statuses_count { get; set; }

        //[DataMember]
        public string time_zone { get; set; }

        [DataMember]
        public string url { get; set; }

        //[DataMember]
        public int utc_offset { get; set; }

        //[DataMember]
        public bool verified { get; set; }


        //[DataMember]
        public string withheld_in_countries { get; set; }
        //[DataMember]
        public string withheld_scope { get; set; }

    }

    public class Entity
    { }
    public class Entity_in_Object
    { }
    public class Place
    { }

    public class OAuth
    {
        private string ConsumerKey { get { return "rbnqym6bymX52AnG1TJcI0iWD"; } }
        private string ConsumerSecret { get { return "LBC7S58eRyJ7DcNtmJMjyBoPs0aMhiXIg2obxmnpmOjI7mfMPs"; } }


        private string RequestKey { get; set; }
        private string RequestSecret { get; set; }

        private string AccessKey { get; set; }
        private string AccessSecret { get; set; }
        public string UserId { get; set; }
        public string ScreenName { get; set; }

        //付随情報
        public Image icon { get; set; }
        public string UserName { get; set; }

        public string PostUrl { get; set; }
        public string[] PostString { get; set; }

        public async Task<string[]> GetRequesttoken()
        {
            string APIURL = "https://api.twitter.com/oauth/request_token";

            var keys = new SortedDictionary<string, string>()
            {
                {"oauth_nonce",GenerateNonce()},
                {"oauth_callback","oob"},
                {"oauth_signature_method","HMAC-SHA1"},
                {"oauth_timestamp",GenerateTimeStamp()},
                {"oauth_consumer_key",ConsumerKey},
                {"oauth_version", "1.0"}
            };

            string signature = GenerateSignature(APIURL, HttpMethod.Post, ConsumerSecret, AccessSecret, keys);
            keys.Add("oauth_signature", signature);

            string RequestURL = "https://api.twitter.com/oauth/request_token?";
            string AuthorizationParms = null;

            foreach (string key in keys.Keys)
            {
                RequestURL += key + "=" + WebUtility.UrlEncode(keys[key]) + "&";
                AuthorizationParms += key + "=\"" + keys[key] + "\", ";
            }
            RequestURL = RequestURL.Remove(RequestURL.Length - 1);
            AuthorizationParms = AuthorizationParms.Remove(AuthorizationParms.Length - 2, 2);

            var request = new HttpRequestMessage();
            var response = new HttpResponseMessage();
            var client = new HttpClient();

            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(RequestURL);
            request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", AuthorizationParms);
            
            request.Content = null;
            

            string result = null;

            response = await client.SendAsync(request);
            var test = request.Headers.AcceptEncoding.ToString();
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                string[] alternative = await GetRequesttoken();
                return alternative;
            }
            byte[] data = await response.Content.ReadAsByteArrayAsync();
            

            //16/4/22UWP移行に伴いGzipの解凍を行います
            using (MemoryStream ms = new MemoryStream())
            {

                byte[] gzBuffer = data;
                int mslength = BitConverter.ToInt32(gzBuffer, 0);
                ms.Write(gzBuffer, 0, gzBuffer.Length -0);
                byte[] Buffer = new byte[mslength];
                ms.Position = 0;
                using (System.IO.Compression.GZipStream zip = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress))
                {
                    zip.Read(Buffer, 0, Buffer.Length);
                }
                result = Encoding.UTF8.GetString(Buffer);
            }


                RequestKey = Regex.Match(result, @"oauth_token=(.*?)&oauth_token_secret=.*?&oauth_callback.*").Groups[1].Value;
            RequestSecret = Regex.Match(result, @"oauth_token=(.*?)&oauth_token_secret=(.*?)&oauth_callback.*").Groups[2].Value;

            PostUrl = "https://api.twitter.com/oauth/authorize?oauth_token="+ RequestKey +
                "&oauth_token_secret=" + RequestSecret;
            var returnresult = new string[3];
            returnresult[0] = PostUrl;
            returnresult[1] = RequestKey;
            returnresult[2] = RequestSecret;
            PostString = returnresult;
            return returnresult;
        }
        public async Task GetAccessKey(string PIN)
        {
            const string APIURL = "https://api.twitter.com/oauth/access_token";
            string ACSURL = "https://api.twitter.com/oauth/access_token?";

            var postdata = new SortedDictionary<string, string>
            {
                {"oauth_consumer_key",ConsumerKey},
                {"oauth_nonce",GenerateNonce()},
                {"oauth_signature_method","HMAC-SHA1"},
                {"oauth_timestamp",GenerateTimeStamp()},
                {"oauth_token",RequestKey},
                {"oauth_version","1.0"},
                {"oauth_verifier",PIN}
            };

            string signature = GenerateSignature(APIURL, HttpMethod.Post, ConsumerSecret, RequestSecret, postdata);
            postdata.Add("oauth_signature", WebUtility.UrlEncode(signature));

            var postquery = new Dictionary<string, string>
            {
                {"oauth_consumer_key",ConsumerKey},
                {"oauth_nonce",postdata["oauth_nonce"]},
                {"oauth_signature_method","HMAC-SHA1"},
                {"oauth_timestamp",postdata["oauth_timestamp"]},
                {"oauth_token",RequestKey},
                {"oauth_version","1.0"},
                {"oauth_signature",WebUtility.UrlEncode(signature)},
                {"oauth_verifier",PIN}
            };

            foreach (string key in postquery.Keys)
            {
                ACSURL += key + "=" + postquery[key] + "&";
            }
            ACSURL = ACSURL.Remove(ACSURL.Length - 1);

            var content = new FormUrlEncodedContent(postdata);
            var request = new HttpRequestMessage(HttpMethod.Post, ACSURL);
            request.Content = content;
            var response = new HttpResponseMessage();
            var client = new System.Net.Http.HttpClient();
            string result = null;

            response = await client.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                byte[] data = await response.Content.ReadAsByteArrayAsync();


                //16/4/22UWP移行に伴いGzipの解凍を行います
                using (MemoryStream ms = new MemoryStream())
                {

                    byte[] gzBuffer = data;
                    int mslength = BitConverter.ToInt32(gzBuffer, 0);
                    ms.Write(gzBuffer, 0, gzBuffer.Length - 0);
                    byte[] Buffer = new byte[mslength];
                    ms.Position = 0;
                    using (System.IO.Compression.GZipStream zip = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress))
                    {
                        zip.Read(Buffer, 0, Buffer.Length);
                    }
                    result = Encoding.UTF8.GetString(Buffer);
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var dialog = new MessageDialog("Twitterアプリ認証に失敗しました再度読み込んでください", "権限が取得できませんでした");
                await dialog.ShowAsync();
            }
            result += "🍸";
            AccessKey = Regex.Match(result, @"oauth_token=(.*?)&oauth_token_secret=(.*?)&user_id=(.*?)&screen_name=(.*?)🍸(.*?)").Groups[1].Value;
            AccessSecret = Regex.Match(result, @"oauth_token=(.*?)&oauth_token_secret=(.*?)&user_id=(.*?)&screen_name=(.*?)🍸(.*?)").Groups[2].Value;
            UserId = Regex.Match(result, @"oauth_token=(.*?)&oauth_token_secret=(.*?)&user_id=(.*?)&screen_name=(.*?)🍸(.*?)").Groups[3].Value;
            ScreenName = Regex.Match(result, @"oauth_token=(.*?)&oauth_token_secret=(.*?)&user_id=(.*?)&screen_name=(.*?)&x(.*?)").Groups[4].Value;

        }

        public void RegisterRequestKey(string requestkey,string requestseret)
        {
            RequestKey = requestkey;
            RequestSecret = requestseret;
        }
        public void RegisterAccessKey(string[] result)
        {
            AccessKey = result[0];
            AccessSecret = result[1];
            ScreenName = result[2];
            UserId = result[3];
        }


        public async Task<string> RestGET(OAuth oauth, string APIURL, SortedDictionary<string, string> postdata)
        {

            var Header = HeaderCreate(APIURL, oauth, HttpMethod.Get, postdata);
            string posturl = APIURL + "?";

            string authorizationHeaderParams = null;

            foreach (string key in Header.Keys)
            {
                posturl += key + "=" + Header[key] + "&";
                authorizationHeaderParams += key + "=\"" + Header[key] + "\", ";
            }
            posturl = posturl.Remove(posturl.Length - 1);
            authorizationHeaderParams = authorizationHeaderParams.Remove(authorizationHeaderParams.Length - 2);

            var request = new HttpRequestMessage();

            var client = new HttpClient();
            request.Method = HttpMethod.Get;
            request.RequestUri = new Uri(posturl);
            request.Headers.Host = "api.twitter.com";
            request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", authorizationHeaderParams);

            var response = await client.SendAsync(request);
            string result = null;
            byte[] data = await response.Content.ReadAsByteArrayAsync();


            //16/4/22UWP移行に伴いGzipの解凍を行います
            using (MemoryStream ms = new MemoryStream())
            {

                byte[] gzBuffer = data;
                int mslength = BitConverter.ToInt32(gzBuffer, 0);
                ms.Write(gzBuffer, 0, gzBuffer.Length - 0);
                byte[] Buffer = new byte[mslength];
                ms.Position = 0;
                using (System.IO.Compression.GZipStream zip = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress))
                {
                    zip.Read(Buffer, 0, Buffer.Length);
                }
                result = Encoding.UTF8.GetString(Buffer);
            }
            return result;
        }
        public async Task<string> RestPOST(OAuth oauth, string APIURL, SortedDictionary<string, string> postdata)
        {
            var postquery = new Dictionary<string, string>(postdata);

            SortedDictionary<string, string> Header =HeaderCreate(APIURL, oauth, HttpMethod.Post, postdata);
            string posturl = APIURL + "?";

            //最初に送るべきパラメーターを追加してからoauth関連のパラメーターを入れる
            postquery.Add("oauth_consumer_key", oauth.ConsumerKey);
            postquery.Add("oauth_nonce", Header["oauth_nonce"]);
            postquery.Add("oauth_timestamp", Header["oauth_timestamp"]);
            postquery.Add("oauth_token", oauth.AccessKey);
            postquery.Add("oauth_signature_method", "HMAC-SHA1");
            postquery.Add("oauth_version", "1.0");
            postquery.Add("oauth_signature", Header["oauth_signature"]);

            string authorizationHeaderParams = null;

            foreach (string key in postquery.Keys)
            {
                posturl += key + "=" + postquery[key] + "&";
                authorizationHeaderParams += key + "=\"" + postquery[key] + "\", ";
            }
            posturl = posturl.Remove(posturl.Length - 1);
            authorizationHeaderParams = authorizationHeaderParams.Remove(authorizationHeaderParams.Length - 2);

            var request = new HttpRequestMessage();

            var client = new HttpClient();
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(posturl);
            request.Headers.Host = "api.twitter.com";
            request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", authorizationHeaderParams);
            var response = await client.SendAsync(request);
            string result = null;
            byte[] data = await response.Content.ReadAsByteArrayAsync();


            //16/4/22UWP移行に伴いGzipの解凍を行います
            using (MemoryStream ms = new MemoryStream())
            {

                byte[] gzBuffer = data;
                int mslength = BitConverter.ToInt32(gzBuffer, 0);
                ms.Write(gzBuffer, 0, gzBuffer.Length - 0);
                byte[] Buffer = new byte[mslength];
                ms.Position = 0;
                using (System.IO.Compression.GZipStream zip = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress))
                {
                    zip.Read(Buffer, 0, Buffer.Length);
                }
                result = Encoding.UTF8.GetString(Buffer);
            }
            return result;
        }

        //ストリーミングに接続
        public async Task<HttpResponseMessage> StreamGet(OAuth oauth, string APIURL, SortedDictionary<string, string> postdata)
        {
            SortedDictionary<string, string> Header = HeaderCreate(APIURL, oauth, HttpMethod.Get, postdata);
            string posturl = APIURL + "?";

            string authorizationHeaderParams = null;

            foreach (string key in Header.Keys)
            {
                posturl += key + "=" + Header[key] + "&";
                authorizationHeaderParams += key + "=\"" + Header[key] + "\", ";
            }
            posturl = posturl.Remove(posturl.Length - 1);
            authorizationHeaderParams = authorizationHeaderParams.Remove(authorizationHeaderParams.Length - 2);

            var request = new HttpRequestMessage();
            var client = new HttpClient();
            request.Method = HttpMethod.Get;
            request.RequestUri = new Uri(posturl);
            request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", authorizationHeaderParams);

            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;

            HttpResponseMessage res = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            return res;
        }

        //テンプレ通り
        public static string GenerateTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }
        public static string GenerateNonce()
        {
            Random random = new Random();
            string strings = "0123456789abcdefghijklmnopqrstuvwxyABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string res = "";
            for (int i = 0; i < 32; i++)
                res += strings[random.Next(0, strings.Length - 1)];
            return res;
        }

        public static string GenerateSignature(string APIURL, HttpMethod type, string ConsumerSecret, string AccessSecret, SortedDictionary<string, string> parms)
        {
            string key = ConsumerSecret + "&" + AccessSecret;

            string data = null;
            foreach (string keys in parms.Keys)
            {
                data += WebUtility.UrlEncode(keys) + "=" + WebUtility.UrlEncode(parms[keys]) + "&";
            }
            data = data.Remove(data.Length - 1);
            data = WebUtility.UrlEncode(data);
            string URL = WebUtility.UrlEncode(APIURL);
            data = type.ToString() + "&" + URL + "&" + data;

            var KeyMaterial = CryptographicBuffer.ConvertStringToBinary(key, BinaryStringEncoding.Utf8);
            var Converter = MacAlgorithmProvider.OpenAlgorithm("HMAC_SHA1");
            var MacKey = Converter.CreateKey(KeyMaterial);

            var DataToBeSigned = CryptographicBuffer.ConvertStringToBinary(data, BinaryStringEncoding.Utf8);
            var SignBuffer = CryptographicEngine.Sign(MacKey, DataToBeSigned);

            string signature = CryptographicBuffer.EncodeToBase64String(SignBuffer);
            return signature;
        }
        public static string GenerateSignature(string APIURL, OAuth oauth, HttpMethod Method, SortedDictionary<string, string> postdata)
        {
            Uri url = new Uri(APIURL);
            string normalizedRequestParameters = null;

            string nonce = GenerateNonce();
            string timestamp = GenerateTimeStamp();

            SortedDictionary<string, string> parameters = postdata;
            parameters.Add("oauth_version", "1.0");
            parameters.Add("oauth_nonce", nonce);
            parameters.Add("oauth_timestamp", timestamp);
            parameters.Add("oauth_signature_method", "HMAC-SHA1");
            parameters.Add("oauth_consumer_key", oauth.ConsumerKey);
            parameters.Add("oauth_token", oauth.AccessKey);

            foreach (string key in parameters.Keys)
            {
                normalizedRequestParameters += key + "=" + parameters[key] + "&";
            }
            normalizedRequestParameters = normalizedRequestParameters.Remove(normalizedRequestParameters.Length - 1);

            string SignatureBase = Method.ToString() + "&" + WebUtility.UrlEncode(APIURL) + "&" + WebUtility.UrlEncode(normalizedRequestParameters);

            string Key = WebUtility.UrlEncode(oauth.ConsumerSecret) + "&" + WebUtility.UrlEncode(oauth.AccessSecret);

            var KeyMaterial = CryptographicBuffer.ConvertStringToBinary(Key, BinaryStringEncoding.Utf8);
            var Converter = MacAlgorithmProvider.OpenAlgorithm("HMAC_SHA1");
            var MacKey = Converter.CreateKey(KeyMaterial);

            var DataToBeSigned = CryptographicBuffer.ConvertStringToBinary(SignatureBase, BinaryStringEncoding.Utf8);
            var SignBuffer = CryptographicEngine.Sign(MacKey, DataToBeSigned);

            string signature = CryptographicBuffer.EncodeToBase64String(SignBuffer);
            return signature;
        }

        public static SortedDictionary<string, string> HeaderCreate(string APIURL, OAuth oauth, HttpMethod Method, SortedDictionary<string, string> postdata)
        {
            Uri url = new Uri(APIURL);
            string normalizedUrl = APIURL;
            string normalizedRequestParameters = null;

            string nonce = GenerateNonce();
            string timestamp = GenerateTimeStamp();

            SortedDictionary<string, string> parameters = postdata;
            parameters.Add("oauth_consumer_key", oauth.ConsumerKey);
            parameters.Add("oauth_token", oauth.AccessKey);
            parameters.Add("oauth_nonce", nonce);
            parameters.Add("oauth_timestamp", timestamp);
            parameters.Add("oauth_version", "1.0");
            parameters.Add("oauth_signature_method", "HMAC-SHA1");

            foreach (string key in parameters.Keys)
            {
                normalizedRequestParameters += key + "=" + parameters[key] + "&";
            }
            normalizedRequestParameters = normalizedRequestParameters.Remove(normalizedRequestParameters.Length - 1);

            StringBuilder signatureBase = new StringBuilder();
            signatureBase.AppendFormat("{0}&", Method.ToString());
            signatureBase.AppendFormat("{0}&", WebUtility.UrlEncode(normalizedUrl));
            signatureBase.AppendFormat("{0}", WebUtility.UrlEncode(normalizedRequestParameters));
            string SignatureBase = signatureBase.ToString();


            string Key = string.Format("{0}&{1}", WebUtility.UrlEncode(oauth.ConsumerSecret), WebUtility.UrlEncode(oauth.AccessSecret));

            var crypt = MacAlgorithmProvider.OpenAlgorithm("HMAC_SHA1");
            var keyBuffer = CryptographicBuffer.CreateFromByteArray(Encoding.UTF8.GetBytes(Key));
            var cryptKey = crypt.CreateKey(keyBuffer);

            var dataBuffer = CryptographicBuffer.CreateFromByteArray(Encoding.UTF8.GetBytes(SignatureBase));
            var signBuffer = CryptographicEngine.Sign(cryptKey, dataBuffer);

            byte[] value;
            CryptographicBuffer.CopyToByteArray(signBuffer, out value);

            string signature = Convert.ToBase64String(value);
            signature = WebUtility.UrlEncode(signature);
            parameters.Add("oauth_signature", signature);
            return parameters;
        }




    }

}
