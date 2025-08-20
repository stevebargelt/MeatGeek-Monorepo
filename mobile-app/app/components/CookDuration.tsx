import React, { useEffect, useState } from "react"
import { TextStyle } from "react-native"
import { Text } from "./Text"
import { colors } from "@/theme/colors"
import { typography } from "@/theme/typography"

interface CookDurationProps {
  startTime: Date
  endTime?: Date
}

export const CookDuration = React.memo(function CookDuration(props: CookDurationProps) {
  const { startTime, endTime } = props
  const [duration, setDuration] = useState(0)

  useEffect(() => {
    const updateDuration = () => {
      const end = endTime || new Date()
      const seconds = Math.floor((end.getTime() - startTime.getTime()) / 1000)
      setDuration(seconds)
    }

    updateDuration()

    // Update every minute for live cooking sessions
    if (!endTime) {
      const timer = setInterval(updateDuration, 60000) // Update every minute
      return () => clearInterval(timer)
    }

    // Return undefined for completed sessions
    return undefined
  }, [startTime, endTime])

  const formatDuration = (seconds: number): string => {
    const hours = Math.floor(seconds / 3600)
    const minutes = Math.floor((seconds % 3600) / 60)

    if (hours > 0) {
      return `${hours}h ${minutes}m`
    }
    return `${minutes}m`
  }

  return <Text style={$durationText}>Cook Time: {formatDuration(duration)}</Text>
})

const $durationText: TextStyle = {
  fontSize: 24,
  fontWeight: "600",
  color: colors.text,
  textAlign: "center",
  marginVertical: 16,
}
