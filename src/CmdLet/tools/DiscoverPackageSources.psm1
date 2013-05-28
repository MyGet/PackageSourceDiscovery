function Add-PackageSource {
    param(
		[parameter(Mandatory = $true)]
		[string]$Name,
		[parameter(Mandatory = $true)]
		[string]$Source
    )   

	$configuration = Get-Content "$env:AppData\NuGet\NuGet.config"
	$configurationXml = [xml]$configuration
	
	if ($configurationXml.configuration.packageSources -eq $null) {
		$configurationXml.configuration.appendChild( $configurationXml.createElement("packageSources") ) | Out-Null
	}
	
	$sourceToAdd = $configurationXml.createElement("add")
	$sourceToAdd.SetAttribute("key", $Name);
	$sourceToAdd.SetAttribute("value", $Source);
	$configurationXml.configuration["packageSources"].appendChild($sourceToAdd) | Out-Null

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

function Remove-PackageSource {
    param(
		[parameter(Mandatory = $true)]
		[string]$Name
    )   

	$configuration = Get-Content "$env:AppData\NuGet\NuGet.config"
	$configurationXml = [xml]$configuration

	$node = $configurationXml.SelectSingleNode("//packageSources/add[@key='$Name']")
	if ($node -ne $null) {
		[Void]$node.ParentNode.RemoveChild($node)

		$configurationXml.save("$env:AppData\NuGet\NuGet.config");
	}
}

function Add-DebuggingSource {
    param(
		[parameter(Mandatory = $true)]
		[string]$Source
    ) 

	$dte.Windows.Item("{ECB7191A-597B-41F5-9843-03A4CF275DDE}").Activate()
	
	$dte.ExecuteCommand("Edit.ClearAll")
	$dte.ExecuteCommand("Debug.EvaluateStatement", ".sympath")
	$dte.ActiveWindow.Selection.SelectAll()
	$previousSources = $dte.ActiveWindow.Selection.Text
	
	if ($previousSources.Contains($Source) -ne $true) {
		$dte.ExecuteCommand("Debug.EvaluateStatement", ".sympath + $Source")
		Write-Host "Added debugging source `"$Source`""
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
		[string]$Title, 
		[parameter(Mandatory = $false)]
		[switch]$OverwriteExisting
    )   

	if ($Url -eq "") {
		$dnsDomains = Get-WmiObject -Class Win32_NetworkAdapterConfiguration -Filter IPEnabled=TRUE -ComputerName . | %{ $_.DNSDomain }
		foreach ($dnsDomain in $dnsDomains) {
			$autoDiscoveryUrl = "http://nuget.$dnsDomain"
			Write-Host "Trying feed autodiscovery from $autoDiscoveryUrl..."
			try {
				Discover-PackageSources -Url $autoDiscoveryUrl -Username $Username -Password $Password -OverwriteExisting $OverwriteExisting
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
	$baseUrl = New-Object System.Uri($Url)

	$resultingMatches = [Regex]::Matches($data, $hrefRegex, "IgnoreCase")
	foreach ($match in $resultingMatches) {
	   $rel = [Regex]::Match($match, $relRegex, "IgnoreCase").Groups[1].Value
	   $href = $match.Groups[1].Value
	   $title = [Regex]::Match($match, $titleRegex, "IgnoreCase").Groups[1].Value

	   if ($rel -eq "nuget") {
			$fullUrl = New-Object System.Uri($baseUrl, $match.Groups[1].Value.Trim())
	        Discover-PackageSources -Url $fullUrl.ToString() -Username $Username -Password $Password -Title $title -OverwriteExisting $OverwriteExisting
	   }
	}

	try {
		$xml = [xml]$data
		
		if ($xml.service -ne $null) {
		    # Regular feed
		    if ($Title -eq "") {
		        $Title = Split-Path $Url -Leaf
		    }
			if ($OverwriteExisting -eq $true) {
				Remove-PackageSource -Name $Title
			}
		    if ((Get-PackageSource -Name $Title) -eq $null) {
		        Add-PackageSource -Name $Title -Source $Url
		    }
		} elseif ($xml.rsd -ne $null) {
		    # RSD document
		    foreach ($service in $xml.rsd.service) {
    			foreach ($endpoint in $service.apis.api) {
    				if ($endpoint.name -eq "nuget-v2-packages" -and $endpoint.preferred -eq "true" ) {
						if ($OverwriteExisting -eq $true) {
							Remove-PackageSource -Name $service.title 
						}
						if ((Get-PackageSource -Name $service.title) -eq $null) {
    				    	Add-PackageSource -Name $service.title -Source $endpoint.apiLink
    				    }
    			    }
					if ($endpoint.name -eq "symsrc-v1-symbols" -and $endpoint.preferred -eq "true" ) {
						Add-DebuggingSource -Source $endpoint.apiLink
					}
    		    }
		    }
		} elseif ($xml.feedList -ne $null) {
		    # NFD document
		    foreach ($feed in $xml.feedList.feed) {
				if ($OverwriteExisting -eq $true) {
					Remove-PackageSource -Name $feed.name
				}
    		    if ((Get-PackageSource -Name $feed.name) -eq $null) {
					$fullUrl = New-Object System.Uri($baseUrl, $feed.url)
    			    Add-PackageSource -Name $feed.name -Source $fullUrl.ToString()
    		    }
		    }
		}
	} catch {
	}
}