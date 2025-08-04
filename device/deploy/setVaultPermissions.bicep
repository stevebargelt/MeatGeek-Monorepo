param functionsApiAppName string
param keyVaultId string
param principalId string
param keyVaultUserRole string

resource kvRoleAssignment 'Microsoft.Authorization/roleAssignments@2021-04-01-preview' = {
  name: guid(keyVaultId, functionsApiAppName, keyVaultUserRole, principalId)
  properties: {
    roleDefinitionId: keyVaultUserRole
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}