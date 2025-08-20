import { FC, useEffect, useState } from "react"
import { ViewStyle, View, FlatList, RefreshControl, Pressable, TextStyle } from "react-native"
import type { AppStackScreenProps } from "@/navigators/AppNavigator"
import { Screen } from "@/components/Screen"
import { Text } from "@/components/Text"
import { Card } from "@/components/Card"
import { useSession } from "@/context/SessionContext"
import { spacing } from "@/theme/spacing"
import { colors } from "@/theme/colors"
import type { SessionSummary } from "@/services/api/types"

interface CookHistoryScreenProps extends AppStackScreenProps<"CookHistory"> {}

export const CookHistoryScreen: FC<CookHistoryScreenProps> = () => {
  const { sessions, isLoading, loadSessions, usesCelsius, error, retryLastAction } = useSession()
  const [refreshing, setRefreshing] = useState(false)

  // Load sessions when screen mounts
  useEffect(() => {
    console.log("CookHistoryScreen mounted, loading sessions...")
    loadSessions()
  }, [])

  const completedSessions = sessions.filter((session) => session.endTime)

  const convertTemp = (temp: number) => {
    return usesCelsius ? Math.round(((temp - 32) * 5) / 9) : Math.round(temp)
  }

  const tempUnit = usesCelsius ? "°C" : "°F"

  const getSessionStatus = (session: SessionSummary) => {
    return session.endTime ? "Completed" : "Active"
  }

  const formatDuration = (session: SessionSummary) => {
    // For now, we'll show a placeholder since SessionSummary doesn't include startTime
    // This would need to be enhanced to either:
    // 1. Include startTime in SessionSummary API response
    // 2. Fetch SessionDetails for duration calculation
    if (!session.endTime) return "In progress"
    return "Duration available in details"
  }

  const onRefresh = async () => {
    setRefreshing(true)
    try {
      await loadSessions()
    } finally {
      setRefreshing(false)
    }
  }

  const renderSessionItem = ({ item }: { item: SessionSummary }) => (
    <Pressable>
      <Card style={$sessionCard} preset="default">
        <View style={$sessionHeader}>
          <Text preset="subheading" text={item.title || "Unnamed Cook"} />
          <Text style={$dateText}>
            {item.endTime ? new Date(item.endTime).toLocaleDateString() : "Active"}
          </Text>
        </View>

        <View style={$sessionDetails}>
          <View style={$leftDetails}>
            <Text style={$statusText}>{getSessionStatus(item)}</Text>
            <Text style={$durationText}>
              {formatDuration(item)}
            </Text>
          </View>
          
          <View style={$rightDetails}>
            <Text style={$typeText}>
              {item.type === "session" ? "BBQ Session" : "Unknown"}
            </Text>
          </View>
        </View>

        <View style={$statusIndicator}>
          <View style={[$statusDot, { backgroundColor: item.endTime ? colors.palette.accent500 : colors.palette.primary400 }]} />
          <Text style={$statusText}>
            {item.endTime ? "Completed" : "In Progress"}
          </Text>
        </View>
        
        {item.endTime && (
          <Text style={$timestampText}>
            Ended: {new Date(item.endTime).toLocaleDateString()} at {new Date(item.endTime).toLocaleTimeString()}
          </Text>
        )}
      </Card>
    </Pressable>
  )

  const renderEmptyState = () => (
    <View style={$emptyState}>
      <Text preset="subheading" text="No Cook History" />
      <Text text="Your completed cooking sessions will appear here." style={$emptyText} />
      <Text text="Start a new cook session to begin tracking your BBQ adventures!" style={$emptySubtext} />
    </View>
  )

  const renderErrorState = () => (
    <View style={$emptyState}>
      <Text preset="subheading" text="Unable to Load History" />
      <Text text={error || "Check your connection and try again."} style={$emptyText} />
      <Pressable style={$retryButton} onPress={retryLastAction}>
        <Text style={$retryButtonText}>Retry</Text>
      </Pressable>
    </View>
  )

  return (
    <Screen style={$root} preset="fixed">
      <View style={$container}>
        <Text preset="heading" text="Cook History" style={$title} />

        <FlatList
          data={completedSessions}
          renderItem={renderSessionItem}
          keyExtractor={(item) => item.id}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
          ListEmptyComponent={error ? renderErrorState : renderEmptyState}
          contentContainerStyle={completedSessions.length === 0 ? $emptyContainer : undefined}
          showsVerticalScrollIndicator={false}
        />
      </View>
    </Screen>
  )
}

const $root: ViewStyle = {
  flex: 1,
}

const $container: ViewStyle = {
  flex: 1,
  padding: spacing.md,
}

const $title: ViewStyle = {
  marginBottom: spacing.md,
}

const $sessionCard: ViewStyle = {
  marginBottom: spacing.sm,
  padding: spacing.md,
}

const $sessionHeader: ViewStyle = {
  flexDirection: "row",
  justifyContent: "space-between",
  alignItems: "center",
  marginBottom: spacing.xs,
}

const $dateText: TextStyle = {
  color: colors.textDim,
  fontSize: 14,
}

const $sessionDetails: ViewStyle = {
  flexDirection: "row",
  justifyContent: "space-between",
  marginBottom: spacing.xs,
}

const $leftDetails: ViewStyle = {
  flex: 1,
}

const $rightDetails: ViewStyle = {
  alignItems: "flex-end",
}

const $proteinText: TextStyle = {
  color: colors.text,
  fontWeight: "500",
}

const $typeText: TextStyle = {
  color: colors.textDim,
  fontSize: 12,
  fontStyle: "italic",
}

const $durationText: TextStyle = {
  color: colors.textDim,
  fontSize: 14,
}

const $temperatureText: TextStyle = {
  color: colors.text,
  fontSize: 14,
  marginBottom: spacing.xs,
}

const $statusIndicator: ViewStyle = {
  flexDirection: "row",
  alignItems: "center",
}

const $statusDot: ViewStyle = {
  width: 8,
  height: 8,
  borderRadius: 4,
  marginRight: spacing.xs,
}

const $statusText: TextStyle = {
  color: colors.textDim,
  fontSize: 12,
}

const $emptyState: ViewStyle = {
  alignItems: "center",
  justifyContent: "center",
  flex: 1,
}

const $emptyContainer: ViewStyle = {
  flex: 1,
  justifyContent: "center",
}

const $emptyText: TextStyle = {
  textAlign: "center",
  marginVertical: spacing.xs,
  color: colors.textDim,
}

const $emptySubtext: TextStyle = {
  textAlign: "center",
  fontSize: 12,
  color: colors.textDim,
  fontStyle: "italic",
  marginTop: spacing.xs,
}

const $timestampText: TextStyle = {
  color: colors.textDim,
  fontSize: 10,
  marginLeft: spacing.xs,
}

const $retryButton: ViewStyle = {
  backgroundColor: colors.palette.accent500,
  paddingHorizontal: spacing.md,
  paddingVertical: spacing.xs,
  borderRadius: 8,
  marginTop: spacing.md,
  alignSelf: "center",
}

const $retryButtonText: TextStyle = {
  color: colors.palette.neutral100,
  fontWeight: "600",
  textAlign: "center",
}
