using System.Xml;
using System.Xml.Linq;
using System.Collections.Concurrent;


// Parse args: read file of RSS links per line.
if (args.Where(arg => !arg.StartsWith("--")).Count() != 1)
{
	Console.WriteLine("csrss rss-link-filepath");
	Console.WriteLine($"  but got {args.Length} args.");
	return 1;
}

// Try read file.
var filepath = args.Where(arg => !arg.StartsWith("--")).First();
string[] lines;
try
{
	lines = File.ReadAllLines(filepath);
}
catch (FileNotFoundException e)
{
	Console.WriteLine($"Could not find/open file {filepath}.");
	return 1;
}

// Just in case, eliminate empty lines.
string[] links = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

if (links.Length == 0)
{
	Console.WriteLine($"File {filepath} contains no links. Exiting, as it's pointless to run an RSS server like this.");
	return 1;
}


// http client to make web requests
var http = new HttpClient();

// set up periodic update
var seconds = 60 * 30;
var ct = new CancellationTokenSource().Token;

// This collects feeds from multiple sources.
ConcurrentDictionary<string, Feed> feeds = new();

// register tasks per link - each refershed individually.
foreach (var link in links)
{
	// run refresh task
	Task.Run(async () =>
	{
		while (!ct.IsCancellationRequested)
		{
			feeds[link] = await GetFeed(link);
			await Task.Delay(TimeSpan.FromSeconds(seconds), ct);
		}
	});
}

// set up web app
var app = WebApplication.Create(args);
app.MapGet("/", (HttpContext ctx) =>
{
	var res = ctx.Response;
	res.StatusCode = 200;
	res.Headers["Content-Type"] = "text/html";
	
	// simple functions to render stuff.
	static XElement e(string elemName, params Object[] objects) => new(elemName, objects);
	static XAttribute attr(string attrName, string attrValue) => new(attrName, attrValue);


	if (feeds.Count == 0)
	{
		return e("html", e("body", e("p", "still scrapin'..."))).ToString();
	}

	// TODO: fix null pointer deref
	var feedItems = feeds.Values.SelectMany(c => c.Channel.Items).ToArray();
	Array.Sort(feedItems, (r, l) => DateTime.Compare(l.PubDate.Value, r.PubDate.Value));
	var items = e("ul",
		feedItems.Where(item => item.Title != null && item.Link != null).Select(item => e("li",
			e("a", attr("href", item.Channel.Link), $"({item.Channel.Title ?? item.Channel.Link})"), 
			e("a", attr("href", item.Link), item.PubDate.Value.ToString("dd/MM/yy "), item.Title)
		))
	);

	var html = new XDocument(e("html", e("body", items)));
	return html.ToString();
});

app.Run();

return 0;


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

