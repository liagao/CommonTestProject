using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Shell;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace XapCopilotDemo2.Properties
{
    /// <summary>
    /// Interaction logic for XapChatWindowControl.
    /// </summary>
    public partial class XapChatWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XapChatWindowControl"/> class.
        /// </summary>
        public XapChatWindowControl()
        {
            this.InitializeComponent();
        }

        private async void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if(e.Key == System.Windows.Input.Key.Enter && string.IsNullOrWhiteSpace(questionBox.Text))
                {
                    e.Handled = true;
                    MessageBox.Show("Please input your question first.", "Xap Copilot", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (e.Key == System.Windows.Input.Key.Enter && !questionBox.IsReadOnly)
                {
                    //string codeSnippet = Regex.Replace(GetCurrentOpenedCodeSnippet(), @"[\r\n]+", string.Empty); 

                    InjectQuestion();

                    var promptMessage = ComposeQuestion();
                    JObject jsonObject = new JObject
                    {
                        //{ "question", $"{questionBox.Text.Trim()} here is the code snippet: ```{codeSnippet}```" }
                        { "question", $"{promptMessage}" }
                    };

                    string question = jsonObject.ToString();

                    questionBox.Text = "Handling the question now, please wait for a while...";
                    questionBox.IsReadOnly = true;

                    if (promptMessage.StartsWith("what is the status of", StringComparison.OrdinalIgnoreCase))
                    {
                        InjectAnswer("Xap is relasing its engine, all component release pipelines are paused now. The ETA of resumption is around 2 hours.");
                    }
                    else
                    {
                        using (var client = new HttpClient())
                        {
                            //client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("sk~dSzDDTv8Vgf5_vkuXxDe4yUXVcawZFiX");
                            client.DefaultRequestHeaders.Add("api-key", "sk~dSzDDTv8Vgf5_vkuXxDe4yUXVcawZFiX");
                            //var myContent = new StringContent($"{{\"question\":\"{question}\"}}");
                            var myContent = new StringContent(question);
                            myContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                            var response = await client.PostAsync("https://cloudgpt.azurewebsites.net/api/cloud-gpt/scenario/xapbot", myContent);

                            var result = await response.Content.ReadAsStringAsync();
                            var json = JsonConvert.DeserializeObject<ResultInfo>(result);
                            InjectAnswer(json.reply);
                        }
                    }

                    questionBox.Text = string.Empty;
                    questionBox.IsReadOnly = false;
                }

            }
            catch (Exception ex)
            {
                InjectException($"Unhandled Exception:{ex}\r\n");
                questionBox.Text = string.Empty;
                questionBox.IsReadOnly = false;
            }

            ScrollViewer.ScrollToEnd();
            questionBox.Focus();
        }

        private string ComposeQuestion()
        {
            return $"{questionBox.Text.Trim()}. " +
                $"If you don't need a code snippet as context, please answer the question directly and ignore the subsequent words of the current prompt message. " +
                $"Otherwise, here is the code snippet for you as context: {Regex.Replace(GetCurrentOpenedCodeSnippet(), @"[\r\n]+", string.Empty)}";
        }

        private string GetCurrentOpenedCodeSnippet()
        {
            IVsTextManager textManager = (IVsTextManager)Package.GetGlobalService(typeof(SVsTextManager));
            int result = textManager.GetActiveView(0, null, out IVsTextView textView);
            if (result == VSConstants.S_OK)
            {
                textView.GetBuffer(out IVsTextLines textLines);
                int lastLine, lastIndex;
                textLines.GetLastLineIndex(out lastLine, out lastIndex);
                textLines.GetLineText(0, 0, lastLine, lastIndex, out string text);
                return text;
            }

            return string.Empty;
        }

        private void InjectException(string ex)
        {
            ConversationGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            ConversationGrid.Children.Add(CreateNewRow(false, ex, ConversationGrid.RowDefinitions.Count - 1));

            ScrollViewer.ScrollToEnd();
        }

        private void InjectAnswer(string reply)
        {
            ConversationGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            ConversationGrid.Children.Add(CreateNewRow(false, reply, ConversationGrid.RowDefinitions.Count - 1));

            ScrollViewer.ScrollToEnd();
        }

        private void InjectQuestion()
        {
            ConversationGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto});
            ConversationGrid.Children.Add(CreateNewRow(true, questionBox.Text, ConversationGrid.RowDefinitions.Count - 1));

            ScrollViewer.ScrollToEnd();
        }

        private Grid CreateNewRow(bool isQuestion, string content, int rowIndex)
        {
            var grid = new Grid();
            grid.SetValue(Grid.RowProperty, rowIndex);
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });


            var icon = new Image();
            var bi = new BitmapImage();

            bi.BeginInit();
            bi.UriSource = new Uri(isQuestion?"/XapCopilotDemo2;component/Properties/Resources/myself.png": "/XapCopilotDemo2;component/Properties/Resources/boticon.png", UriKind.RelativeOrAbsolute);
            bi.EndInit();
            icon.Source = bi;
            icon.SetValue(Grid.RowProperty, 0);
            icon.SetValue(Grid.ColumnProperty, 0);
            icon.VerticalAlignment = VerticalAlignment.Top;
            icon.Margin = new Thickness(0, 10, 0, 0);

            var stackPanel = new StackPanel();
            stackPanel.Margin = new Thickness(0, 10, 0, 10);
            stackPanel.SetValue(Grid.RowProperty, 0);
            stackPanel.SetValue(Grid.ColumnProperty, 1);

            var name = new TextBlock();
            name.Margin = new Thickness(2, 5, 0, 5);
            name.FontWeight = FontWeights.Bold;
            name.HorizontalAlignment = HorizontalAlignment.Stretch;
            name.Text = isQuestion ?  "Lianyong Gao" : "Xap Copilot";

            var contentBox = new TextBox();
            contentBox.Margin = new Thickness(0, 5, 0, 5);
            contentBox.IsReadOnly = true;
            contentBox.Background = Brushes.Transparent;
            contentBox.BorderBrush = Brushes.Transparent;
            contentBox.BorderThickness= new Thickness(0);
            contentBox.TextWrapping = TextWrapping.Wrap;
            contentBox.HorizontalAlignment = HorizontalAlignment.Stretch;
            contentBox.Text = content;


            if (isQuestion)
            {
                stackPanel.Background = Brushes.LightSteelBlue;
            }

            stackPanel.Children.Add(name);
            stackPanel.Children.Add(contentBox);

            grid.Children.Add(icon);
            grid.Children.Add(stackPanel);

            return grid;
        }

        private void questionBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if(questionBox.Text == "Please ask your questions here...")
            {
                questionBox.Text = string.Empty;
                questionBox.FontStyle = FontStyles.Normal;
                questionBox.Foreground = Brushes.Black;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.ToString());
        }

        private void questionBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(questionBox.Text))
            {
                questionBox.Text = "Please ask your questions here...";
                questionBox.FontStyle = FontStyles.Italic;
                questionBox.Foreground = Brushes.Gray;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ConversationGrid.Children.Clear();
        }
    }

    public class ResultInfo
    {
        public string reply { get; set; }
    }
}