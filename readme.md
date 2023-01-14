# Meat Geek - Azure Environment

## Resource Groups

If we are recreating from scratch, create:

```shell

az group create -n MeatGeek-Sessions -l northcentralus
az group create -n MeatGeek-Proxy -l northcentralus
az group create -n MeatGeek-Device -l northcentralus
az group create -n MeatGeek-IoT -l northcentralus
az group create -n MeatGeek-Shared -l northcentralus

# Undecided:
# az group create -n MeatGeek-Events -l centralus
# IoT probably replaces Events in our app

```

## MeatGeek Proxies

[![Build Status](https://dev.azure.com/stevenbargelt/MeatGeek/_apis/build/status/stevebargelt.meatgeek-azure-proxies?branchName=master)](https://dev.azure.com/stevenbargelt/MeatGeek/_build/latest?definitionId=9&branchName=master)

The front-end for all of the APIs. This allows a consistent URL and interface for all of the MeatGeek related APIs. The main idea of the proxy is that we can switch out the back-end without changing the URLs. This is implemented as a Function App with no functions.

mgsessapibrbwfchhgocgf

https://meatgeek-proxy.azurewebsites.net/sessions => https://mgsessapibrbwfchhgocgf.azurewebsites.net/api/sessions
https://mgsessapibrbwfchhgocgf.azurewebsites.net/api/sessions/{sessionsid}

### Azure Pipeline

[Azure DevOps Pipeline](https://dev.azure.com/stevenbargelt/MeatGeek%20Proxy/_build)

### Code

[Github](https://github.com/stevebargelt/meatgeek-azure-proxies)

## MeatGeek Sessions API

[![Build Status](https://dev.azure.com/stevenbargelt/MeatGeek%20Sessions%20API/_apis/build/status/stevebargelt.meatgeek-azure-proxies?branchName=master)](https://dev.azure.com/stevenbargelt/MeatGeek%20Sessions%20API/_build/latest?definitionId=9&branchName=master)

The API associated with Sessions. Sessions could also be called cooks, smokes, or BBQs. When you are actively cooking something on your grill / BBQ that is a session. Sessions have their own API and data storage. As you might imagine Sessions become associated with IoT data like temperature, and grill/BBQ status.

### Azure Pipeline

[Azure DevOps Pipeline](https://dev.azure.com/stevenbargelt/MeatGeek%20Sessions%20API/_build)

### Code / Github

[Github](https://github.com/stevebargelt/meatgeek-azure-sessions)

## MeatGeek Device API

[![Build Status](https://dev.azure.com/stevenbargelt/MeatGeek%20Device%20API/_apis/build/status/stevebargelt.meatgeek-azure-deviceapi?branchName=master)](https://dev.azure.com/stevenbargelt/MeatGeek%20Device%20API/_build/latest?definitionId=14&branchName=master)

The Device API is an Azure Function App that uses an Azure Relay Service to communicate directly with devices.

### Azure Pipeline

[Azure DevOps Pipeline](https://dev.azure.com/stevenbargelt/MeatGeek%20Device%20API/_build)

### Code / Github

[Github](https://github.com/stevebargelt/meatgeek-azure-deviceapi)

## MeatGeek IoT

### Azure Pipeline

### Code / Github

## MeatGeek Shared

[![Build Status](https://dev.azure.com/stevenbargelt/MeatGeek%20Shared/_apis/build/status/stevebargelt.MeatGeek-Shared?branchName=main)](https://dev.azure.com/stevenbargelt/MeatGeek%20Shared/_build/latest?definitionId=15&branchName=main)

This one is Private on github and Azure DevOps. It creates the Azure App Config service and populates the App Config values.

To update or add values, edit deploy/app-config.json and push to Github. The Release Pipeline in Azure DevOps updates / adds the values.

### Azure Pipeline

[Azure DevOps Pipeline](https://dev.azure.com/stevenbargelt/MeatGeek%20Shared/_build)

### Code / Github

### ELMS - Logging and Monitoring

IMPORTANT: ELMS will be configured to capture all logs from the edge modules. To change this behavior, you can go to the Configuration section of the Function App 'iotedgelogsapp-d589c907' and update the regular expression for the app setting 'LogsRegex'.

IMPORTANT: You must update device twin for your IoT edge devices with "tags.logPullEnabled='true'" to collect logs from their modules.

<!-- Undecided:

## MeatGeek Event Hub

### Azure Pipeline

### Code / Github -->
