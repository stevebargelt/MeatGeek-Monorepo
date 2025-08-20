import { FC, useEffect } from "react"
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
  const { sessions, isLoading, loadSessions, usesCelsius } = useSession()

  // Don't call loadSessions here - it's already called by ActiveCookScreen
  // useEffect(() => {
  //   loadSessions()
  // }, [])

  const completedSessions = sessions.filter((session) => session.endTime)

  const convertTemp = (temp: number) => {
    return usesCelsius ? Math.round(((temp - 32) * 5) / 9) : Math.round(temp)
  }

  const tempUnit = usesCelsius ? "°C" : "°F"

  const getSessionStatus = (session: SessionSummary) => {
    return session.endTime ? "Completed" : "Active"
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
          <Text style={$statusText}>{getSessionStatus(item)}</Text>
        </View>

        <View style={$statusIndicator}>
          <View style={[$statusDot, { backgroundColor: colors.palette.accent500 }]} />
          <Text style={$statusText}>Completed</Text>
        </View>
      </Card>
    </Pressable>
  )

  const renderEmptyState = () => (
    <View style={$emptyState}>
      <Text preset="subheading" text="No Cook History" />
      <Text text="Your completed cooking sessions will appear here." />
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
          refreshControl={<RefreshControl refreshing={isLoading} onRefresh={loadSessions} />}
          ListEmptyComponent={renderEmptyState}
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

const $proteinText: TextStyle = {
  color: colors.text,
  fontWeight: "500",
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
