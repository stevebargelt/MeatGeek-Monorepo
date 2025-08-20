# MeatGeek Mobile App

A React Native iOS application for managing BBQ cooking sessions, built with Infinite Red's Ignite framework.

![MeatGeek Logo](./assets/images/logo.png)

## 🚀 Features

### MVP Features (v1.0)

- **Multi-probe temperature monitoring** - Track grill + up to 4 meat probes
- **Real-time session management** - Start, monitor, and end cooking sessions
- **Cook duration tracking** - Live elapsed time display
- **Custom probe labeling** - "Brisket Point", "Flat", "Ribs", etc.
- **Temperature unit toggle** - Fahrenheit/Celsius support
- **Temperature presets** - Quick buttons for 225°F, 275°F, 350°F
- **Auto-refresh** - Updates every 30 seconds
- **Pull-to-refresh** - Manual refresh on all screens
- **Offline capability** - Works with cached data
- **Cook history** - View completed cooking sessions
- **Haptic feedback** - Premium iOS interactions
- **Error recovery** - Retry buttons and graceful failure handling

### Technical Features

- **iOS/iPad optimized** - Native feel and performance
- **API integration** - MeatGeek Sessions API
- **Session persistence** - Survives app restarts
- **Mock data fallback** - Development without backend
- **TypeScript** - Type-safe development
- **Responsive design** - Works on iPhone and iPad

## 🛠 Setup & Development

### Prerequisites

- Node.js 18+
- Yarn package manager
- iOS development environment (Xcode)
- iPhone/iPad simulator or physical device

### Installation

```bash
# Navigate to mobile app directory
cd mobile-app

# Install dependencies
yarn install

# Start development server
yarn start

# Run on iOS simulator
yarn ios

# Run on physical device (requires Expo Go app)
# Scan QR code from yarn start
```

### Environment Configuration

The app uses different configurations for development and production:

- **Development**: Uses mock data for testing without API dependency
- **Production**: Connects to live MeatGeek Sessions API

API configuration is in `app/config/`:

- `config.dev.ts` - Development settings with mock data
- `config.prod.ts` - Production API endpoints
- `config.base.ts` - Shared configuration

### Testing with Real API

To test with the production Sessions API:

1. Start the test IoT device: `../../iot-edge/start-test-device.sh`
2. Update `app/context/SessionContext.tsx` to set `useMockData = false`
3. Restart the app

## 📱 App Architecture

### Technology Stack

- **React Native 0.79+** - Mobile framework
- **TypeScript 5** - Type safety
- **Ignite Framework** - Boilerplate and tooling
- **React Navigation 7** - Screen navigation
- **React Context** - State management
- **Apisauce** - HTTP client
- **MMKV** - Local storage
- **Expo Haptics** - iOS haptic feedback

### Project Structure

```
mobile-app/
├── app/
│   ├── components/          # Reusable UI components
│   │   ├── TemperatureCard.tsx
│   │   ├── CookDuration.tsx
│   │   └── ConnectionStatus.tsx
│   ├── screens/             # Main application screens
│   │   ├── ActiveCookScreen.tsx
│   │   ├── StartCookScreen.tsx
│   │   └── CookHistoryScreen.tsx
│   ├── context/             # State management
│   │   └── SessionContext.tsx
│   ├── services/            # API and external services
│   │   └── api/
│   ├── config/              # Environment configuration
│   ├── theme/               # Design system
│   └── navigators/          # Navigation setup
├── assets/                  # Images and icons
├── ios/                     # iOS native code
└── package.json
```

### State Management

The app uses React Context for state management:

- **SessionContext** - Manages cooking sessions, API calls, and app state
- **MMKV Storage** - Persists data locally for offline use
- **Auto-refresh** - Keeps temperature data current

### API Integration

- **Base URL**: `https://meatgeeksessionsapi.azurewebsites.net`
- **Endpoints**:
  - `GET /api/sessions/{smokerId}` - Get all sessions
  - `POST /api/sessions/{smokerId}` - Start new session
  - `PATCH /api/endsession/{smokerId}/{sessionId}` - End session
  - `GET /api/sessions/statuses/{smokerId}/{sessionId}` - Get current status/temps

## 🧪 Testing

### Manual Testing Checklist

- [ ] **Create cook with custom name** ("Test Brisket #1")
- [ ] **Label probes with custom names** ("Point", "Flat")
- [ ] **Toggle between Fahrenheit and Celsius**
- [ ] **Use temperature preset buttons** (225°F, 275°F, 350°F)
- [ ] **View elapsed cook time** (updates every minute)
- [ ] **Haptic feedback on temperature targets reached**
- [ ] **Auto-refresh works every 30 seconds**
- [ ] **Pull-to-refresh works on all screens**
- [ ] **Session persists when app is closed and reopened**
- [ ] **Connection status indicator updates correctly**
- [ ] **Error screens show retry buttons**
- [ ] **App works offline with cached data**
- [ ] **View cook history with meaningful names**

### Testing with Mock Data

By default, the app runs with mock data in development mode. This includes:

- 1 active cooking session ("Memorial Day Brisket")
- 3 completed historical sessions
- Simulated temperature variations
- Network delay simulation

### Testing with Production API

1. Update `SessionContext.tsx`: Set `useMockData = false`
2. Ensure test device is running: `../../iot-edge/start-test-device.sh`
3. Restart the app
4. Create a new cooking session
5. Monitor real temperature data

## 🚀 Deployment

### iOS Deployment (Future)

The app is ready for iOS deployment with:

- Proper bundle identifier: `com.mobileapp`
- iOS optimized assets and icons
- Native haptic feedback integration
- Apple HIG compliant design

### Next Steps for Production

1. **Code Signing** - Set up Apple Developer certificates
2. **Fastlane** - Configure automated deployment
3. **TestFlight** - Beta testing with real users
4. **App Store** - Production release

## 🔧 Configuration

### API Configuration

Update `app/config/config.prod.ts` for production deployment:

```typescript
export default {
  API_URL: "https://meatgeeksessionsapi.azurewebsites.net",
  SMOKER_ID: "your-production-smoker-id",
}
```

### Mock Data Toggle

To switch between mock and real data, update `app/context/SessionContext.tsx`:

```typescript
const [useMockData] = useState(false) // Set to false for production API
```

## 📈 Performance

### Optimization Features

- **MMKV storage** - Fast local persistence
- **Optimized re-renders** - React.memo components
- **Intelligent auto-refresh** - Only when session is active
- **One-time alerts** - Prevents excessive notifications
- **Battery efficient** - 30-second refresh intervals

## 🐛 Known Issues & Limitations

### MVP Limitations

- **iOS only** - Android support planned for v2
- **Single smoker** - Multi-device support planned
- **No user authentication** - Planned for v2
- **No push notifications** - Local alerts only
- **Basic charts** - Advanced analytics planned

## 🎯 Future Roadmap

### v1.1 Enhancements

- Real-time WebSocket connections
- Advanced temperature charts
- Push notifications
- Multiple smoker support

### v2.0 Features

- User authentication
- Social features (cook sharing)
- Recipe integration
- Apple Watch companion app
- Advanced analytics and recommendations

## 📚 Resources

### Documentation

- [Ignite Framework](https://github.com/infinitered/ignite)
- [React Navigation](https://reactnavigation.org/)
- [Expo Haptics](https://docs.expo.dev/versions/latest/sdk/haptics/)
- [MeatGeek API Documentation](../../sessions/README.md)

---

Built with ❤️ using Infinite Red's Ignite framework for the MeatGeek BBQ platform.
