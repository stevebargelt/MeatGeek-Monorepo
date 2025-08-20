import React, { useRef } from "react"
import { View, ViewStyle, TextStyle } from "react-native"
import { Text } from "./Text"
import { Card } from "./Card"
import { colors } from "@/theme/colors"
import { spacing } from "@/theme/spacing"
import { typography } from "@/theme/typography"

export interface ProbeDisplay {
  probeName: string // "Grill", "Probe 1", "Probe 2", etc.
  currentTemp: number | null
  targetTemp: number | null
  isActive: boolean
}

interface TemperatureCardProps {
  probe: ProbeDisplay
  usesCelsius: boolean
  onTargetReached?: () => void
}

export const TemperatureCard = React.memo(function TemperatureCard(props: TemperatureCardProps) {
  const { probe, usesCelsius, onTargetReached } = props
  const alertTriggeredRef = useRef(false)

  const convertTemp = (temp: number) => {
    return usesCelsius ? Math.round(((temp - 32) * 5) / 9) : Math.round(temp)
  }

  const tempUnit = usesCelsius ? "°C" : "°F"

  const getTemperatureColor = () => {
    if (!probe.currentTemp || !probe.targetTemp) return colors.textDim
    const current = convertTemp(probe.currentTemp)
    const target = convertTemp(probe.targetTemp)
    const diff = Math.abs(current - target)

    if (diff <= 3) {
      // Only trigger alert once when target is reached
      if (!alertTriggeredRef.current) {
        alertTriggeredRef.current = true
        onTargetReached?.()
      }
      return colors.palette.accent500 // Green-ish
    } else {
      // Reset alert when temperature moves away from target
      alertTriggeredRef.current = false
    }

    if (diff <= 10) return colors.palette.primary400 // Orange-ish
    return colors.palette.secondary500 // Red-ish
  }

  const getStatusText = () => {
    if (!probe.currentTemp || !probe.targetTemp) return "--"
    const current = convertTemp(probe.currentTemp)
    const target = convertTemp(probe.targetTemp)
    const diff = current - target

    if (Math.abs(diff) <= 3) return "At Target"
    return diff > 0 ? `+${diff}${tempUnit}` : `${diff}${tempUnit}`
  }

  return (
    <Card style={$card} preset="default">
      <View style={$content}>
        <Text style={$probeLabel} text={probe.probeName} />
        <View style={$temperatureRow}>
          <Text style={[$currentTemp, { color: getTemperatureColor() }]}>
            {probe.currentTemp ? convertTemp(probe.currentTemp) : "--"}
            {tempUnit}
          </Text>
          <View style={$targetInfo}>
            <Text style={$targetLabel}>
              Target: {probe.targetTemp ? convertTemp(probe.targetTemp) : "--"}
              {tempUnit}
            </Text>
            <Text style={[$statusText, { color: getTemperatureColor() }]}>{getStatusText()}</Text>
          </View>
        </View>
      </View>
    </Card>
  )
})

const $card: ViewStyle = {
  marginVertical: spacing.xs,
  paddingHorizontal: spacing.sm,
  paddingVertical: spacing.sm,
}

const $content: ViewStyle = {
  flex: 1,
}

const $probeLabel: TextStyle = {
  fontSize: 16,
  fontWeight: "600",
  marginBottom: spacing.xs,
}

const $temperatureRow: ViewStyle = {
  flexDirection: "row",
  alignItems: "center",
  justifyContent: "space-between",
}

const $currentTemp: TextStyle = {
  fontSize: 32,
  fontWeight: "800",
}

const $targetInfo: ViewStyle = {
  alignItems: "flex-end",
}

const $targetLabel: TextStyle = {
  fontSize: 12,
  color: colors.textDim,
}

const $statusText: TextStyle = {
  fontSize: 16,
  fontWeight: "600",
}
