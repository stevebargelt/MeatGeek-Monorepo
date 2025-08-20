import React from "react"
import { View, ViewStyle, TextStyle } from "react-native"
import { Text } from "./Text"
import { colors } from "@/theme/colors"
import { spacing } from "@/theme/spacing"
import { typography } from "@/theme/typography"

interface ConnectionStatusProps {
  isConnected: boolean
  lastRefresh?: Date
}

export const ConnectionStatus = React.memo(function ConnectionStatus(props: ConnectionStatusProps) {
  const { isConnected, lastRefresh } = props

  const formatLastUpdate = (date?: Date) => {
    if (!date) return "Never"
    return date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })
  }

  return (
    <View style={$container}>
      <View
        style={[
          $statusDot,
          { backgroundColor: isConnected ? colors.palette.accent500 : colors.palette.secondary500 },
        ]}
      />
      <Text style={$statusText}>
        {isConnected ? "Connected" : "Offline"} • Last update: {formatLastUpdate(lastRefresh)}
      </Text>
    </View>
  )
})

const $container: ViewStyle = {
  flexDirection: "row",
  alignItems: "center",
  padding: spacing.sm,
  backgroundColor: colors.background,
  borderBottomWidth: 1,
  borderBottomColor: colors.border,
}

const $statusDot: ViewStyle = {
  width: 8,
  height: 8,
  borderRadius: 4,
  marginRight: spacing.xs,
}

const $statusText: TextStyle = {
  fontSize: 12,
  color: colors.textDim,
}
