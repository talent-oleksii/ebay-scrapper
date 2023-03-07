using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using HtmlAgilityPack;
using System.Net;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Security.Policy;
using static System.Net.Mime.MediaTypeNames;

namespace Scrapping
{
    public partial class Form1 : Form
    {

        public bool folderNameWithItem;
        public Form1()
        {
            InitializeComponent();
            folderNameWithItem = false;
        }

        private string GetRealUrl (string url)
        {
            HtmlWeb web = new HtmlWeb ();
            HtmlAgilityPack.HtmlDocument doc = web.Load(url);
            HtmlNode picturePanelSection = doc.DocumentNode.SelectSingleNode("//div[@id='PicturePanel']");
            string realUrl = picturePanelSection.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            return realUrl;
        }

        private List<string> GetHtmlContent (string url, ref string folderName)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument doc = web.Load(url);
            List<string> lines = new List<string>();
            // Get Title, Subtitle
            HtmlNode titleSection = doc.DocumentNode.SelectSingleNode("//h1[@class]");
            if (titleSection == null) return lines;
            foreach (HtmlNode node in titleSection.SelectNodes(".//span[@class]"))
            {
                string title = node.InnerText;
                lines.Add("Title : " + title);
                if (folderNameWithItem)
                {
                    folderName = title;
                    Directory.CreateDirectory(folderName);
                }
                break;
            }
            HtmlNode subtitleSection = doc.DocumentNode.SelectSingleNode("//h2[@class='x-item-title__sellerProvidedTitle']");
            string subtitle = subtitleSection.SelectSingleNode(".//span[@class]").InnerText;
            lines.Add("Subtitle : " + subtitle);

            // Get Price, Recommended Price and Currency
            HtmlNode priceCurrnecySection = doc.DocumentNode.SelectSingleNode("//div[@class='x-bin-price__content']");
            if (priceCurrnecySection != null)
            {
                foreach (HtmlNode node2 in priceCurrnecySection.SelectNodes(".//span[@itemprop]"))
                {
                    string attributeValue = node2.GetAttributeValue("itemprop", "");
                    if (attributeValue == "price")
                    {
                        string price = node2.GetAttributeValue("content", "");
                        lines.Add("Price : " + price);
                        int priceValue = 0;
                        foreach(var letter in price)
                        {
                            priceValue = priceValue * 10 + letter - '0';
                        }
                        priceValue /= 2;
                        lines.Add("Recommended price : " + priceValue.ToString());
                    }
                    else if (attributeValue == "priceCurrency")
                    {
                        string currency = node2.GetAttributeValue("content", "");
                        lines.Add("Currency : " + currency);
                    }
                }
            }

            // Get Options

            // Get Options - Condition
            lines.Add("Options:");
            HtmlNode conditionSection = doc.DocumentNode.SelectSingleNode("//div[@class='vim x-about-this-item']");
            if (conditionSection != null)
            {
                HtmlNodeCollection featureLabelCollection = conditionSection.SelectNodes(".//div[@class='ux-labels-values__labels']");
                if (featureLabelCollection != null)
                {
                    List<string> featureLabelList = new List<string>();
                    int featureIndex = 0;
                    foreach (HtmlNode node in featureLabelCollection)
                    {
                        string featureLabel = Regex.Replace(node.SelectSingleNode(".//span[contains(@class, 'ux-textspans')]").WriteTo(), "<.*?>", "").Trim();
                        featureLabelList.Add(featureLabel);
                    }
                    HtmlNodeCollection featureValueCollection = conditionSection.SelectNodes(".//div[@class='ux-labels-values__values']");
                    if (featureValueCollection != null)
                    {
                        foreach (HtmlNode node in featureValueCollection)
                        {
                            string featureValue = Regex.Replace(node.SelectSingleNode(".//span[contains(@class, 'ux-textspans')]").WriteTo(), "<.*?>", "").Trim();
                            lines.Add(featureLabelList[featureIndex].Replace(":", " : ") + featureValue);
                            featureIndex++;
                        }
                    }
                }
            }

            // Download All Images
            
            HtmlNode carouselSection = doc.DocumentNode.SelectSingleNode("//div[@class='ux-image-carousel']");
            if (carouselSection != null)
            {
                HtmlNodeCollection imageCollection = carouselSection.SelectNodes(".//div[@class]");
                if (imageCollection != null)
                {
                    int imageCount = 0;
                    foreach (HtmlNode node in imageCollection)
                    {
                        string imageSource = "";
                        if (node.SelectSingleNode(".//img[@src]") != null)
                        {
                            imageSource = node.SelectSingleNode(".//img[@src]").GetAttributeValue("src", "");
                        }
                        if (node.SelectSingleNode(".//img[@data-src]") != null)
                        {
                            imageSource = node.SelectSingleNode(".//img[@data-src]").GetAttributeValue("data-src", "");
                        }
                        if (imageSource != "")
                        {
                            using (WebClient client = new WebClient())
                            {
                                client.DownloadFile(imageSource.Replace("500.jpg", "1600.jpg"),
                                    folderName + "/" + imageCount + ".jpg");
                            }
                            imageCount++;
                        }
                    }
                }
            }
            

            // Get Description
            HtmlNode descriptionSection = doc.DocumentNode.SelectSingleNode("//div[@id='desc_wrapper_ctr']");
            if (descriptionSection != null)
            {
                HtmlNode descriptionTitleSection = descriptionSection.SelectSingleNode(".//h2[@class='d-item-description__title']");
                if (descriptionTitleSection != null)
                {
                    string descriptionTitle = descriptionTitleSection.SelectSingleNode(".//span[@class='ux-textspans']").InnerText;
                    lines.Add(descriptionTitle);
                    HtmlNode descriptionDetailSection = descriptionSection.SelectSingleNode(".//iframe[@id='desc_ifr']");
                    if (descriptionDetailSection != null)
                    {
                        string descriptionDetailUrl = descriptionDetailSection.GetAttributeValue("src", "");
                        HtmlWeb web1 = new HtmlWeb();
                        HtmlAgilityPack.HtmlDocument doc1 = web1.Load(descriptionDetailUrl);
                        HtmlNode descriptionDetailTableSection = doc1.DocumentNode.SelectSingleNode("//main[@class]");
                        if (descriptionDetailTableSection != null)
                        {
                            string descriptionDetailTableText = descriptionDetailTableSection.WriteTo();
                            string descriptionDetailText = Regex.Replace(descriptionDetailTableText, "<.*?>", "").Trim();
                            string filteredDescriptionDetailText = descriptionDetailText.Replace("\n\n\n", "").Replace("Exterior", "").Replace("Interior", "").Replace("Engine", "").Replace("Download the eBay Motors app", "");
                            lines.Add(filteredDescriptionDetailText);
                            HtmlNodeCollection descriptionImageCollection = doc1.DocumentNode.SelectNodes("//img[@src]");
                            
                            int extraImageCount = 0;
                            if (descriptionDetailTableSection != null)
                            {
                                Directory.CreateDirectory(folderName + "/EXTRA");
                                foreach (HtmlNode node in descriptionImageCollection)
                                {
                                    string imageSource = node.GetAttributeValue("src", "");
                                    using (WebClient client = new WebClient())
                                    {
                                        client.DownloadFile(imageSource.Replace("$_1.JPG", "$_57.JPG"),
                                            folderName + "/EXTRA/" + extraImageCount + ".jpg");
                                    }
                                    extraImageCount++;
                                }
                            }
                            
                        }
                    }
                }
            }
            return lines;
        }
        private void DoScrapping(string url, string folderName)
        {
            if(folderNameWithItem == false)
                Directory.CreateDirectory(folderName);
            List<string> lines = new List<string>();
            lines = GetHtmlContent(GetRealUrl(url), ref folderName);
            using (StreamWriter outputFile = new StreamWriter(folderName + "/0.html"))
            {
                outputFile.Write("<p>");
                foreach (string line in lines)
                {
                    outputFile.Write(line);
                    outputFile.Write("<br>");
                }
                outputFile.Write("</p>");

            }
        }
        private string GetUrl(string url)
        {
            string res = "";
            using (var reader = new StreamReader("list.csv"))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                    if (values[0] == url)
                    {
                        res = values[1];
                        break;
                    }
                }
            }
            return res;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            foreach (string line in textBox1.Lines)
            {
                string url = GetUrl(line);
                if (url == "") continue;
                if(folderNameWithItem == false)
                    DoScrapping(url, line);
                else
                    DoScrapping(url, line);
            }
            MessageBox.Show("Complete");
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            folderNameWithItem = false;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            folderNameWithItem = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var csv = new StringBuilder();
            string countString = this.textBox2.Text;
            int count = 0;
            foreach (char c in countString)
            {
                count = count * 10 + c - '0';
            }
            for(int i = 1; i <= count; i++)
            {
                string url = "https://www.ebay.com/b/Cars-Trucks/6001?For%2520Sale%2520By=Private%2520Seller&LH_BIN=1&LH_Complete=1&LH_ItemCondition=3000%7C1000%7C2500&LH_PrefLoc=1&Model%2520Year=1900%7C1980%7C1979%7C1978%7C1977%7C1976%7C1975%7C1974%7C1973%7C1972%7C1971%7C1970%7C1969%7C1968%7C1967%7C1966%7C1965%7C1964%7C1963%7C1962%7C1961%7C1960%7C1959%7C1958%7C1957%7C1956%7C1955%7C1954%7C1953%7C1952%7C1951%7C1950%7C1949%7C1948%7C1947%7C1946%7C1945%7C1942%7C1941%7C1940%7C1939%7C1938%7C1937%7C1936%7C1935%7C1934%7C1933%7C1932%7C1931%7C1930%7C1929%7C1928%7C1923%7C1916%7C1909%7C1908%7C1901&mag=1&rt=nc&_dmd=1&_fsrp=0&_sacat=6001&_stpos=95125%2D5904&_udlo=20000";

                HtmlWeb web = new HtmlWeb();
                HtmlAgilityPack.HtmlDocument doc = web.Load(url);
                MessageBox.Show(doc.DocumentNode.WriteTo());
                
                // MessageBox.Show(url.Replace("$$$$$", i.ToString()));
                // MessageBox.Show(doc.DocumentNode.WriteTo());
                // string str = doc.DocumentNode.SelectSingleNode("//div[@class='pagecontainer']").WriteTo();
                // HtmlNode ULSection = doc.DocumentNode.SelectSingleNode("//div[@class='pagecontainer']").SelectSingleNode(".//div[@class='container']").SelectSingleNode(".//ul[@class='b-list__items_nofooter']");
                // MessageBox.Show(ULSection.WriteTo());
                // HtmlNodeCollection LISectionCollection = ULSection.SelectNodes(".//li[@class]");
                // MessageBox.Show(LISectionCollection.Count.ToString());
                /*
                foreach(HtmlNode LISection in LISectionCollection)
                {
                    HtmlNode linkSection = LISection.SelectSingleNode(".//a[@href]");
                    string link = linkSection.GetAttributeValue("href", "");
                    // https://www.ebay.com/itm/195540346431?hash=item2d871ce63f:g:L6wAAOSwoHNjsOZf -> 195540346431
                    string item = link.Replace("https://www.eba.com/itm/", "");
                    item = link.Split('?')[0];
                    var newLine = string.Format("{0},{1}", item, link);
                    MessageBox.Show(newLine);
                    csv.AppendLine(newLine);
                }
                */
            }
            File.WriteAllText("list.csv", csv.ToString());
            MessageBox.Show("Complete");
        }

        private void label2_Click(object sender, EventArgs e)
        {
        }
    }
}
