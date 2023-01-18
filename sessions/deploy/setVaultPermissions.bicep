//param keyVaultName string
param keyVaultId string
param principalId string
param functionsApiAppName string
param keyVaultUserRole string
// param subId string
// param rg string

resource kvFunctionAppPermissions 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(keyVaultId, functionsApiAppName, keyVaultUserRole)
  properties: {
    principalId: principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: keyVaultUserRole
  }
}
