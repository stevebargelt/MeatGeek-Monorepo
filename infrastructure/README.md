# MeatGeek Infrastructure Orchestration

This directory contains the complete system deployment orchestration for the MeatGeek IoT platform.

## Quick Start

### GitHub Actions (Recommended)
1. Go to **Actions** → **Deploy Complete MeatGeek System**
2. Click **Run workflow**
3. Select environments and type `DEPLOY` to confirm
4. Monitor deployment progress

### Local Deployment
```bash
cd infrastructure
export AZURE_SUBSCRIPTION_ID="your-subscription-id"
export AZURE_OBJECT_ID="your-azure-ad-object-id"
./deploy.sh
```

### Direct Azure CLI
```bash
cd infrastructure
az deployment sub create \
  --location northcentralus \
  --template-file main.bicep \
  --parameters \
    location=northcentralus \
    objectId=your-object-id \
    environmentsTodeploy=prod-staging
```

## Files

| File | Purpose |
|------|---------|
| `main.bicep` | Master orchestration template |
| `deploy.sh` | Local deployment script |
| `DEPLOYMENT.md` | Complete deployment guide |
| `README.md` | This quick reference |

## Environment Options

- `prod-only` - Production only
- `prod-staging` - Production + Staging  
- `prod-staging-test` - Production + Staging + Test
- `all` - All environments

## Architecture

The deployment creates:
- **1 Shared Resource Group** with Cosmos DB, Key Vault, Container Registry
- **3-12 Environment-Specific Resource Groups** for Function Apps
- **Complete microservices infrastructure** for Sessions, Device, and IoT APIs

See `DEPLOYMENT.md` for detailed documentation.

## Cost Estimate

- **Production only**: ~$10-20/month
- **Production + Staging**: ~$15-30/month
- **All environments**: ~$25-50/month

## Support

- 📖 **Full Documentation**: See `DEPLOYMENT.md`
- 🐛 **Issues**: Open GitHub issue
- 💬 **Questions**: Check repository discussions