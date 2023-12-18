using PuppeteerSharp;
using System.Net.Http;
using System.Threading.Tasks;

await main();

async Task main()
{
    Console.WriteLine("ENTER SONG URL: ");
    Console.WriteLine("EX: https://audiomack.com/{ARTIST}/song/{SONG NAME}");
    String? url = Console.ReadLine();

    if (String.IsNullOrEmpty(url))
    {
        Console.WriteLine("ERROR : URL WAS EMPTY");
        return;
    }

    //Set up the browser 
    using var browserFetcher = new BrowserFetcher();
    await browserFetcher.DownloadAsync();
    var browser = await Puppeteer.LaunchAsync(new LaunchOptions
    {
        Headless = true
    });
    var page = await browser.NewPageAsync();

    //Process each request on the page 
    await page.SetRequestInterceptionAsync(true);
    page.Request += async (sender, e) =>
    {
        //Look for the audio file url 
        if (e.Request.Url.Contains("https://music.audiomack.com/"))
        {
            Console.WriteLine("INFO : GOT URL TO DOWNLOAD FROM");
            byte[] response = await sendGetRequest(e.Request.Url);
            Console.WriteLine("INFO : WRITING AUDIO FILE");
            await writeAudioFile(response);
            Console.WriteLine("INFO : DONE");
        }

        await e.Request.ContinueAsync();
    };

    //Go to song page 
    await page.GoToAsync(url);

    //Wait for page to load
    await page.WaitForNetworkIdleAsync();
    await page.WaitForSelectorAsync("#maincontent > div._Container_1h5nn_1._maxWidth--xl_1h5nn_41 > div > section > div._FlexColumn_q3asn_14 > div.music-showcase_MusicShowcaseWrapper__jpYXy > div > div:nth-child(2) > div > div._PlayerControls_tc5lm_66.player-controls > span > button");

    //Get the play button from the dom and click it 
    var playButton = await page.QuerySelectorAsync("#maincontent > div._Container_1h5nn_1._maxWidth--xl_1h5nn_41 > div > section > div._FlexColumn_q3asn_14 > div.music-showcase_MusicShowcaseWrapper__jpYXy > div > div:nth-child(2) > div > div._PlayerControls_tc5lm_66.player-controls > span > button");
    await playButton.ClickAsync();

    //Wait for network idle
    await page.WaitForNetworkIdleAsync();

    //Close Browser 
    await browser.CloseAsync();
}


async Task<byte[]> sendGetRequest(string url)
{
    using HttpClient client = new HttpClient();
    HttpResponseMessage response = await client.GetAsync(url);
    response.EnsureSuccessStatusCode();
    byte[] responseBody = await response.Content.ReadAsByteArrayAsync();
    return responseBody;
}

async Task writeAudioFile(byte[] fileContent)
{
    await File.WriteAllBytesAsync("output.m4a", fileContent);
}