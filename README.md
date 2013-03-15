# NuGet Feed Discovery 1.0.0-alpha001

NuGet Feed Discovery (NFD) allows for NuGet-based clients tools to discover the feeds that are hosted by a user or organization.

NuGet Feed Discovery is an attempt to remove friction from the following scenarios:

* An individual user may have several NuGet feeds spread across the Internet. Some may be on NuGet.org (including curated feeds), some on MyGet and maybe some on my corporate network. How do I easily point my Visual Studio to all my feeds accross different machines? And how do I maintain this configuration?
* An organization may have several feeds internally as well as one on MyGet and some CI packages on TeamCity. How can this organization tell his developers what feeds they can/should use?
* An organization may have a NuGet server containing multiple feeds. How will developers in this organization get a list of available feeds and services?

For both scenarios, a simple feed discovery mechanism could facilitate this. Such feed discovery mechanism could be any URL out there (even multiple per host).

As an example, we've implemented the above. Open Visual Studio and open any solution. Then issue the following in the Package Manager Console:

```powershell
Install-Package DiscoverPackageSources
Discover-PackageSources -Url "http://www.myget.org/gallery"
```

Close and re-open Visual Studio and check your package sources. The URL has been verified for a NFD manifest URL and the manifest has been parsed. Matching feeds have been installed into the NuGet.config file, in this case all feeds listed in the MyGet gallery.

## Request

An NFD request is an HTTP GET to an NDF manifest file with optional authentication and an optional NuGet-ApiKey HTTP Header. There are no filtering or searching options at this time.

## Response

The response will be an XML document following the **Really Simple Discovery** (RSD) RFC as described on https://github.com/danielberlinger/rsd. Since not all required metadata can be obtained from the RSD format, the [Dublin Core schema](http://dublincore.org/documents/2012/06/14/dcmi-terms/?v=elements) is present in the NFD response as well.

Vendors and open source projects are allowed to add their own schema to the NFD discovery document however the manifest described below should be respected at all times.

An example manifest could be:

```xml
<?xml version="1.0" encoding="utf-8"?>
<rsd version="1.0" xmlns:dc="http://purl.org/dc/elements/1.1/">
  <service>
    <engineName>MyGet</engineName>
    <engineLink>http://www.myget.org</engineLink>
    
    <dc:identifier>http://www.myget.org/F/googleanalyticstracker</dc:identifier>    
    <dc:creator>maartenba</dc:creator>    
    <dc:owner>maartenba</dc:owner>
    <dc:title>Staging feed for GoogleAnalyticsTracker</dc:title>
    <dc:description>Staging feed for GoogleAnalyticsTracker</dc:description> 
    <homePageLink>http://www.myget.org/gallery/googleanalyticstracker</homePageLink>
        
    <apis>
      <api name="nuget-v2-packages" preferred="true" apiLink="http://www.myget.org/F/googleanalyticstracker/api/v2" blogID="" />
      <api name="nuget-v2-push" preferred="true" apiLink="http://www.myget.org/F/googleanalyticstracker/api/v2/package" blogID="">
        <settings>
          <setting name="apiKey">abcdefghijkl</setting>
        </settings>
      </api>
      <api name="nuget-v1-packages" preferred="false" apiLink="http://www.myget.org/F/googleanalyticstracker/api/v1" blogID="" />
    </apis>
  </service>
</rsd>
```

The following table describes the elements in the data format:

<table>
<tr>
<th>Element</th>
<th>Description</th>
</tr>
<tr>
<td>engineName</td>
<td>Optional: the software on which the feed is running</td>
</tr>
<tr>
<td>engineLink</td>
<td>Optional: the URL to the website of the software on which the feed is running</td>
</tr>
<tr>
<td>dc:identifier</td>
<td><b>Required</b> - a globally unique URN/URL identifying he feed</td>
</tr>
<tr>
<td>dc:owner</td>
<td>Optional: the owner of the feed</td>
</tr>
<tr>
<td>dc:creator</td>
<td>Optional: the creator of the feed</td>
</tr>
<tr>
<td>dc:title</td>
<td><b>Required</b> - feed name</td>
</tr>
<tr>
<td>dc:description</td>
<td>Optional: feed description</td>
</tr>
<tr>
<td>homePageLink</td>
<td>Optional: link to an HTML representation of the feed</td>
</tr>
<tr>
<td><span style="padding-left:10px">apis</span></td>
<td><b>Required</b> - a list of endpoints for the feed</td>
</tr>
<tr>
<td><span style="padding-left:20px">name</span></td>
<td>
<b>Required</b> - the feed protocol used<br/><br/>
Proposed values:<br/>
<ul>
<li>nuget-v1-packages (Orchard)</li>
<li>nuget-v2-packages (current NuGet protocol)</li>
<li>nuget-v2-push (package push URL)</li>
<li>symbols-v1 (current SymbolSource implementation)</li>
</ul>
Note that this list can be extended at will (e.g. chocolatey-v1 or proget-v2). Clients can decide which versions they support.
</td>
</tr>
<tr>
<td><span style="padding-left:20px">preferred</span></td>
<td><b>Required</b> - is this the preferred endpoint to communicate with the feed?</td>
</tr>
<tr>
<td><span style="padding-left:20px">apiLink</span></td>
<td><b>Required</b> - the GET URL</td>
</tr>
<tr>
<td><span style="padding-left:20px">blogID</span></td>
<td><b>Required</b> - for compatibility with RSD spec, not used: leave BLANK</td>
</tr>
</table>

The `<api />` element can contain zero, one or more `<setting name="key">value</setting>` elements specifying settings such as `requireAuthentication`, `apiKey` and vendor-specific settings.

## Server Implementation Guidelines

### Feed Uniqueness

Feed uniqueness must be global. `urn:<guid>` or the feed URL can be valid identifiers.

All feeds should contain an Id.

### Feed Visibility / Access Control

All feeds that the requesting user has read access to should be returned. If the user is anonymous, feeds that require authentication should be omitted.

If the user is logged in, either controlled by basic authentication or using the `NuGet-ApiKey` HttpHeader, the server should return every feed the user has access to as well as feed specific settings such as API keys and so on. The NFD client can use these specifics to preconfigure the NuGet.config file on the user's machine.

## Client / Consumer Implementation Guidelines

The client should respect the following flow of discovering feeds:

* If no NFD is given, the client should assume `http://nuget.<currentdomain>` as the NFD server.
* The NFD URL is accesses and downloaded. It can contain:
  * HTML containing a tag such as 

	```html
    <link rel="nuget" 
          type="application/rsd+xml" 
          title="GoogleAnalyticsTracker feed on MyGet" 
          href="http://myget.org/discovery/feed/googleanalyticstracker"/>
	```

	This URL should be followed and the NFD manifest parsed. Note that multiple tags may exist and should all be parsed.
	
  * HTML containing a tag such as

	```html
    <link rel="nuget" 
    	  type="application/atom+xml" 
    	  title="WebMatrix Package Source" 
    	  href="http://nuget.org/api/v2/curated-feeds/WebMatrix/Packages"/>
	```

	This URL should be treated as a NuGet feed and added as-is, using the title attribute as the feed's title in NuGet package source list. No further metadata can be discovered for this feed. Note that multiple tags may exist.
	
  * If the URL directly points to a NuGet Feed Discovery Manifest, we can immediately parse it.

The client should support entry of a complete NFD URL `<protocol>://<host name>:<port>/<path>` but require only that the host name be entered. When less than a full URL is entered, the client should verify if the host returns a NFD manifest or contains a `<link rel="nuget"/>` tag.

Depending on security, consuming an NFD manifest using the `NuGet-ApiKey` header or using basic authentication may yield additional endpoints and API settings. For example, MyGet produces the following manifest on an anonymous call to https://www.myget.org/Discovery/Feed/googleanalyticstracker:

```xml
<rsd version="1.0" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns="http://archipelago.phrasewise.com/rsd">
  <service>
    <engineName>MyGet</engineName>
    <engineLink>http://www.myget.org/</engineLink>
    <dc:identifier>http://www.myget.org/F/googleanalyticstracker/</dc:identifier>
    <dc:owner>maartenba</dc:owner>
    <dc:creator>maartenba</dc:creator>
    <dc:title>Staging feed for GoogleAnalyticsTracker</dc:title>
    <dc:description>Staging feed for GoogleAnalyticsTracker</dc:description>
    <homePageLink>http://www.myget.org/Feed/Details/googleanalyticstracker/</homePageLink>
    <apis>
      <api name="nuget-v2-packages" blogID="" preferred="true" apiLink="http://www.myget.org/F/googleanalyticstracker/" />
      <api name="nuget-v1-packages" blogID="" preferred="false" apiLink="http://www.myget.org/F/googleanalyticstracker/api/v1/" />
    </apis>
  </service>
</rsd>
```

The authenticated version of https://www.myget.org/Discovery/Feed/googleanalyticstracker yields:

```xml
<rsd version="1.0" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns="http://archipelago.phrasewise.com/rsd">
  <service>
    <engineName>MyGet</engineName>
    <engineLink>http://www.myget.org/</engineLink>
    <dc:identifier>http://www.myget.org/F/googleanalyticstracker/</dc:identifier>
    <dc:owner>maartenba</dc:owner>
    <dc:creator>maartenba</dc:creator>
    <dc:title>Staging feed for GoogleAnalyticsTracker</dc:title>
    <dc:description>Staging feed for GoogleAnalyticsTracker</dc:description>
    <homePageLink>http://www.myget.org/Feed/Details/googleanalyticstracker/</homePageLink>
    <apis>
      <api name="nuget-v2-packages" blogID="" preferred="true" apiLink="http://www.myget.org/F/googleanalyticstracker/" />
      <api name="nuget-v1-packages" blogID="" preferred="false" apiLink="http://www.myget.org/F/googleanalyticstracker/api/v1/" />
      <api name="nuget-v2-push" blogID="" preferred="true" apiLink="http://www.myget.org/F/googleanalyticstracker/">
        <settings>
          <setting name="apiKey">530c14a6-6ce5-47d0-8f14-9daab627aa38</setting>
        </settings>
      </api>
      <api name="symbols-v2-push" blogID="" preferred="true" apiLink="http://nuget.gw.symbolsource.org/MyGet/googleanalyticstracker">
        <settings>
          <setting name="apiKey">abcdefghijklmnop</setting>
        </settings>
      </api>
    </apis>
  </service>
</rsd>
```

## Example Client / Consumer implementation

The following Client/Consumer implementation only discovers feed endpoints for consuming packages. Push, symbols and API setting elements are ignored. A package producer should probably discover push endpoints as well and store the provided API key for future use.
The Client/Consumer is implemented using PowerShell.

```powershell
function Add-PackageSource {
    param(
  	[parameter(Mandatory = $true)]
		[string]$Name,
		[parameter(Mandatory = $true)]
		[string]$Source
    )   

	$configuration = Get-Content "$env:AppData\NuGet\NuGet.config"
	$configurationXml = [xml]$configuration

	$sourceToAdd = $configurationXml.createElement("add")
	$sourceToAdd.SetAttribute("key", $Name);
	$sourceToAdd.SetAttribute("value", $Source);
	$configurationXml.configuration.packageSources.appendChild($sourceToAdd) | Out-Null

	$configurationXml.save("$env:AppData\NuGet\NuGet.config")

	Write-Host "Added feed `"$Name`" ($Source)"
}

function Get-PackageSource {
    param(
		[parameter(Mandatory = $false)]
		[string]$Name
    )   
	
	$configuration = Get-Content "$env:AppData\NuGet\NuGet.config"
	$configurationXml = [xml]$configuration
	
	if ($Name -ne $null -and $Name -ne "") {
		return $configurationXml.configuration.packageSources.add | where { $_.key -eq $Name} | Format-Table @{Label="Name"; Expression={$_.key}}, @{Label="Source"; Expression={$_.value}}
	} else {
		return $null
	}
}

function Discover-PackageSources {
    param(
		[parameter(Mandatory = $false)]
		[string]$Url,
		[parameter(Mandatory = $false)]
		[string]$Username, 
		[parameter(Mandatory = $false)]
		[string]$Password, 
		[parameter(Mandatory = $false)]
		[string]$ApiKey, 
		[parameter(Mandatory = $false)]
		[string]$Title
    )   

	if ($Url -eq "") {
		$dnsDomains = Get-WmiObject -Class Win32_NetworkAdapterConfiguration -Filter IPEnabled=TRUE -ComputerName . | %{ $_.DNSDomain }
		foreach ($dnsDomain in $dnsDomains) {
			$autoDiscoveryUrl = "http://nuget.$dnsDomain"
			Write-Host "Trying feed autodiscovery from $autoDiscoveryUrl..."
			try {
				Discover-PackageSources -Url $autoDiscoveryUrl -Username $Username -Password $Password
			} catch {
				Write-Host "Could not autodiscover feeds."
			}
		}
		return
	}
	
	$relRegex = "<\s*link\s*[^>]*?rel\s*=\s*[`"']*([^`"'>]+)[^>]*?>"
	$hrefRegex = "<\s*link\s*[^>]*?href\s*=\s*[`"']*([^`"'>]+)[^>]*?>"
	$titleRegex = "<\s*link\s*[^>]*?title\s*=\s*[`"']*([^`"'>]+)[^>]*?>"

	$data = $null

	$webclient = new-object System.Net.WebClient
	if ($Username -ne "") {
	   $credentials = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($Username + ":" + $Password))
        $webClient.Headers.Add("Authorization", "Basic $credentials")
	}
	if ($ApiKey -ne "") {
		$webClient.Headers.Add("X-NuGet-ApiKey", $ApiKey) # for backwards compatibility
		$webClient.Headers.Add("NuGet-ApiKey", $ApiKey)
	}
 
	$data = $webclient.DownloadString($Url)

	$resultingMatches = [Regex]::Matches($data, $hrefRegex, "IgnoreCase")
	foreach ($match in $resultingMatches) {
	   $rel = [Regex]::Match($match, $relRegex, "IgnoreCase").Groups[1].Value
	   $href = $match.Groups[1].Value
	   $title = [Regex]::Match($match, $titleRegex, "IgnoreCase").Groups[1].Value

	   if ($rel -eq "nuget") {
	       Discover-PackageSources -Url $match.Groups[1].Value.Trim() -Username $Username -Password $Password -Title $title
	   }
	}
	
	try {
		$xml = [xml]$data
		
		if ($xml.service -ne $null) {
		    # Regular feed
		    if ($Title -eq "") {
		        $Title = Split-Path $Url -Leaf
		    }
		    if ((Get-PackageSource -Name $Title) -eq $null) {
		        Add-PackageSource -Name $Title -Source $Url
		    }
		} elseif ($xml.rsd -ne $null) {
		    # RSD document
		    foreach ($service in $xml.rsd.service) {
    		    if ((Get-PackageSource -Name $service.title) -eq $null) {
    			    foreach ($endpoint in $service.apis.api) {
    				    if ($endpoint.name -eq "nuget-v2-packages" -and $endpoint.preferred -eq "true" ) {
    				    	Add-PackageSource -Name $service.title -Source $endpoint.apiLink
    				    }
    			    }
    		    }
		    }
		}
	} catch {
	}
}
```
