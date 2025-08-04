param keyVaultName string
param secretName string
param secretValue string

resource secret 'Microsoft.KeyVault/vaults/secrets@2021-11-01-preview' = {
  name: '${keyVaultName}/${secretName}'
  properties: {
    value: secretValue
  }
}