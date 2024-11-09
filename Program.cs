using System.Xml;
using System.Xml.Linq;

// http client to make web requests
var http = new HttpClient();

// set up periodic update
var seconds = 60 * 30;
var ct = new CancellationTokenSource().Token;
Feed? feed = null;

// run refresh loop.
Task.Run(async () =>
{
	while (!ct.IsCancellationRequested)
	{
		feed = await GetFeed("https://ro-che.info/articles/rss.xml");
		await Task.Delay(TimeSpan.FromSeconds(seconds), ct);
	}
});

// set up web app
var app = WebApplication.Create(args);
app.MapGet("/", (HttpContext ctx) =>
{
	var res = ctx.Response;
	res.StatusCode = 200;
	res.Headers["Content-Type"] = "text/html";
	
	static XElement e(string elemName, params Object[] objects) => new(elemName, objects);
	static XAttribute attr(string attrName, string attrValue) => new(attrName, attrValue);


	if (feed == null)
	{
		return e("html", e("body", e("p", "still scrapin'..."))).ToString();
	}

	// TODO: fix null pointer deref
	Array.Sort(feed.Channel.Items, (r, l) => DateTime.Compare(l.PubDate.Value, r.PubDate.Value));
	var items = e("ul",
		feed.Channel.Items.Where(item => item.Title != null && item.Link != null).Select(item => e("li", 
			e("a", attr("href", item.Link), item.PubDate.Value.ToString("dd/MM/yy "), item.Title)
		))
	);

	var html = new XDocument(new XElement("html", new XElement("body", items)));
	return html.ToString();
});

app.Run();


async Task<Feed> GetFeed(string uri)
{
	using var response = await http.GetAsync(uri);
	if (!response.IsSuccessStatusCode)
	{
		return Feed.Failed(response.ToString());
	}
	
	var content = await response.Content.ReadAsStringAsync();
	return new(content);
}

