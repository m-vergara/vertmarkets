using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace VertMarketsConsoleApp
{
    class Program
    {
        static HttpClient client = new HttpClient();
        static List<GoodSubscribers> goodsubs = new List<GoodSubscribers>();
        
        static void Main(string[] args)
        {
            RunAsync().Wait();
        }
        class TokenObj
        {
            public bool IsApiCall { get; set; }
            public string Token { get; set; }
        }
        class Subscriber
        {
            public string Id { get; set; }
            public string FirstName { get; set; }
            public string lastName { get; set; }
            public List<int> MagazineIds { get; set; }

        }
        class CategoryObj
        {
            public IList<string> data { get; set; }           
        }
        
        class Magazine
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Category { get; set; }
        }
        class Magazines
        {
            public List<Magazine> data { get; set; }
        }
        class MagazineCategory
        {
            public int Id { get; set; }
            public string category { get; set; }
        }
         class Subscribers
        {
            public List<Subscriber> data { get; set; }
        }
        class MagIds
        {
            public List<int> Ids { get; set; }
        }
        class GoodSubscribers
        {
            public string Id { get; set; }
        }
        static async Task RunAsync()
        {
            string subscriberIds = null;
            client.BaseAddress = new Uri("http://magazinestore.azurewebsites.net/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //get token
            HttpResponseMessage response = await client.GetAsync("api/token");
            if (response.IsSuccessStatusCode)
            {
                var token = await response.Content.ReadAsAsync<TokenObj>();

                //get categories
                response = await client.GetAsync("api/categories/" + token.Token);
                var categoryjsonStr = await response.Content.ReadAsStringAsync();
                var categories = JsonConvert.DeserializeObject<CategoryObj>(categoryjsonStr);
                List<MagazineCategory> magList = new List<MagazineCategory>();
                //loop thru each category and get list of magazines.
                foreach (var c in categories.data)
                {
                    response = await client.GetAsync("api/magazines/" + token.Token + "/" + c);
                    var magazineStr = await response.Content.ReadAsStringAsync();
                    var magazines = JsonConvert.DeserializeObject<Magazines>(magazineStr);
                    foreach (var m in magazines.data)
                    {
                        MagazineCategory mag = new MagazineCategory();
                        mag.Id = m.Id;
                        mag.category = m.Category;
                        magList.Add(mag);
                    }
                    
                }
                //get subscribers
                response = await client.GetAsync("api/subscribers/"+ token.Token);
                MagIds subMagIds = new MagIds();
                var jsonStr = await response.Content.ReadAsStringAsync();
                var subs = JsonConvert.DeserializeObject<Subscribers>(jsonStr);
                var catType = magList.GroupBy(x => x.category);
                bool isGoodSubscriber = false;
                //loop thru subscribers and check if theyre subscribe to at least one magazine each category.
                foreach (var s in subs.data) { 
                    subMagIds.Ids = s.MagazineIds.ToList();
                    foreach (var c in catType) {
                        var singleCategory = magList.Where(o => o.category == c.Key);
                         bool isFoundSubscriber = singleCategory.Any(row => subMagIds.Ids.Contains(row.Id));
                        if (isFoundSubscriber)
                        {
                            isGoodSubscriber = true;
                        }
                        else
                        {
                            isGoodSubscriber = false;
                            break;
                        }
                        
                    }
                    if (isGoodSubscriber)
                    {                    
                        subscriberIds +=   "'" + s.Id.ToString() + "',";
                    }
                }
                subscriberIds = subscriberIds.TrimEnd(',');
                string postUrl = "api/answer/" + token.Token;
                var jsonString = @"{ 'subscribers': [" + subscriberIds + "]}";
                var stringContent = new StringContent(JsonConvert.SerializeObject(jsonString), Encoding.UTF8, "application/json");
   
                response = await client.PostAsync(postUrl, stringContent);
                var resultobj = response.Content.ReadAsStringAsync();
              
                Console.WriteLine(resultobj.Result);
            }
        }
    }
}
