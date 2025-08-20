import { FC, useEffect } from "react"
import { ViewStyle, ScrollView, RefreshControl, View, TextStyle } from "react-native"
import * as Haptics from "expo-haptics"
import { useNavigation } from "@react-navigation/native"
import type { AppStackScreenProps } from "@/navigators/AppNavigator"
import { Screen } from "@/components/Screen"
import { Text } from "@/components/Text"
import { Button } from "@/components/Button"
import { TemperatureCard } from "@/components/TemperatureCard"
import { CookDuration } from "@/components/CookDuration"
import { ConnectionStatus } from "@/components/ConnectionStatus"
import { useSession } from "@/context/SessionContext"
import { spacing } from "@/theme/spacing"

interface ActiveCookScreenProps extends AppStackScreenProps<"ActiveCook"> {}

export const ActiveCookScreen: FC<ActiveCookScreenProps> = () => {
  const navigation = useNavigation()
  const {
    activeSession,
    currentStatus,
    probeDisplays,
    isLoading,
    isConnected,
    lastRefresh,
    error,
    usesCelsius,
    loadSessions,
    loadSessionStatus,
    endActiveSession,
    toggleTemperatureUnit,
    retryLastAction,
    clearError,
  } = useSession()

  // Load sessions on mount only (remove activeSession dependency to prevent infinite loop)
  useEffect(() => {
    loadSessions()
  }, []) // Only run on mount

  // Separate effect for auto-refresh timer
  useEffect(() => {
    if (!activeSession) return

    const timer = setInterval(() => {
      console.log("Auto-refreshing temperatures...")
      loadSessionStatus(activeSession.Id)
    }, 30000) // 30 seconds

    return () => clearInterval(timer)
  }, [activeSession?.Id]) // Only depend on the session ID, not the whole object

  const onRefresh = async () => {
    await loadSessions()
    if (activeSession) {
      await loadSessionStatus(activeSession.Id)
    }
  }

  const handleTargetReached = async () => {
    console.log("Temperature target reached!")
    // Trigger haptic feedback for iOS
    Haptics.notificationAsync(Haptics.NotificationFeedbackType.Success)

    // TODO: Add sound alert in future version
    // For MVP, haptic feedback provides sufficient notification
  }

  const handleEndSession = async () => {
    Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Medium)
    const success = await endActiveSession()
    if (success) {
      console.log("Session ended successfully")
      Haptics.notificationAsync(Haptics.NotificationFeedbackType.Success)
    }
  }

  const handleToggleTemperatureUnit = () => {
    Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Light)
    toggleTemperatureUnit()
  }

  return (
    <Screen style={$root} preset="fixed">
      <ConnectionStatus isConnected={isConnected} lastRefresh={lastRefresh || undefined} />

      <ScrollView
        refreshControl={<RefreshControl refreshing={isLoading} onRefresh={onRefresh} />}
        contentContainerStyle={$scrollContent}
      >
        <Text preset="heading" text="Current Cook" />

        {error && (
          <View style={$errorContainer}>
            <Text text={error} style={$errorText} />
            <Button text="Retry" onPress={retryLastAction} />
            <Button text="Dismiss" preset="reversed" onPress={clearError} />
          </View>
        )}

        {activeSession ? (
          <View>
            <Text preset="subheading" text={activeSession.Title || "Unnamed Cook"} />
            <CookDuration
              startTime={new Date(activeSession.StartTime || activeSession.TimeStamp)}
              endTime={activeSession.EndTime ? new Date(activeSession.EndTime) : undefined}
            />

            {/* Multi-probe temperature display */}
            {probeDisplays.length > 0 ? (
              probeDisplays
                .filter((p) => p.isActive)
                .map((probe, index) => (
                  <TemperatureCard
                    key={`${probe.probeName}-${index}`}
                    probe={probe}
                    usesCelsius={usesCelsius}
                    onTargetReached={handleTargetReached}
                  />
                ))
            ) : (
              <View style={$noDataContainer}>
                <Text text="No temperature data available" />
                <Text text="Device may not be active or connected" style={$subText} />
              </View>
            )}

            <Button
              text={`Toggle to ${usesCelsius ? "Fahrenheit" : "Celsius"}`}
              preset="reversed"
              onPress={handleToggleTemperatureUnit}
              style={$button}
            />
            <Button text="End Cook" onPress={handleEndSession} style={$button} />
          </View>
        ) : (
          <View>
            <Text text="No active cook session" />
            <Button
              text="Start New Cook"
              onPress={() => (navigation as any).navigate("StartCook")}
              style={$button}
            />
          </View>
        )}

        <Button
          text="View History"
          preset="reversed"
          onPress={() => (navigation as any).navigate("CookHistory")}
          style={$button}
        />
      </ScrollView>
    </Screen>
  )
}

const $root: ViewStyle = {
  flex: 1,
}

const $scrollContent: ViewStyle = {
  padding: spacing.md,
}

const $button: ViewStyle = {
  marginTop: spacing.sm,
}

const $errorContainer: ViewStyle = {
  backgroundColor: "#FEE",
  padding: spacing.sm,
  borderRadius: 8,
  marginVertical: spacing.sm,
}

const $errorText: TextStyle = {
  color: "#A00",
  marginBottom: spacing.xs,
}

const $noDataContainer: ViewStyle = {
  alignItems: "center",
  padding: spacing.lg,
  backgroundColor: "#F8F8F8",
  borderRadius: 8,
  marginVertical: spacing.md,
}

const $subText: TextStyle = {
  fontSize: 14,
  color: "#666",
  marginTop: spacing.xs,
}
