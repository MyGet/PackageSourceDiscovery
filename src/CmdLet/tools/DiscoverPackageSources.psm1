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