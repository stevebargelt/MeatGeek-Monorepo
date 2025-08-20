import { FC, useState } from "react"
import { ViewStyle, View, Alert, TextStyle } from "react-native"
import * as Haptics from "expo-haptics"
import { useNavigation } from "@react-navigation/native"
import type { AppStackScreenProps } from "@/navigators/AppNavigator"
import { Screen } from "@/components/Screen"
import { Text } from "@/components/Text"
import { Button } from "@/components/Button"
import { TextField } from "@/components/TextField"
import { useSession } from "@/context/SessionContext"
import { spacing } from "@/theme/spacing"
import Config from "@/config"
import type { CreateSessionRequest } from "@/services/api/types"

interface StartCookScreenProps extends AppStackScreenProps<"StartCook"> {}

export const StartCookScreen: FC<StartCookScreenProps> = () => {
  const navigation = useNavigation()
  const { startSession, isLoading, usesCelsius, temperaturePresets } = useSession()

  const [title, setTitle] = useState("")
  const [description, setDescription] = useState("")

  const handleStartCook = async () => {
    if (!title.trim()) {
      Haptics.notificationAsync(Haptics.NotificationFeedbackType.Error)
      Alert.alert("Error", "Please enter a cook title")
      return
    }

    Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Medium)

    const sessionData: CreateSessionRequest = {
      smokerId: Config.SMOKER_ID,
      title: title.trim(),
      description: description.trim() || `${title.trim()} cooking session`,
      startTime: new Date().toISOString(),
    }

    const success = await startSession(sessionData)
    if (success) {
      Haptics.notificationAsync(Haptics.NotificationFeedbackType.Success)
      navigation.goBack()
    } else {
      Haptics.notificationAsync(Haptics.NotificationFeedbackType.Error)
      Alert.alert("Error", "Failed to start cooking session")
    }
  }

  return (
    <Screen style={$root} preset="scroll">
      <View style={$container}>
        <Text preset="heading" text="Start New Cook" />

        <TextField
          label="Cook Title"
          placeholder="Memorial Day Brisket"
          value={title}
          onChangeText={setTitle}
          style={$field}
        />

        <TextField
          label="Description"
          placeholder="Long cook brisket with point and flat monitoring"
          value={description}
          onChangeText={setDescription}
          multiline={true}
          style={$field}
        />

        <Text style={$note}>
          Note: Temperature monitoring and probe configuration will be handled by the smoker device.
          This creates the cooking session that your device will track.
        </Text>

        <Button
          text={isLoading ? "Starting..." : "Start Cook"}
          onPress={handleStartCook}
          disabled={isLoading}
          style={$button}
        />
        <Button
          text="Cancel"
          preset="reversed"
          onPress={() => navigation.goBack()}
          style={$button}
        />
      </View>
    </Screen>
  )
}

const $root: ViewStyle = {
  flex: 1,
}

const $container: ViewStyle = {
  padding: spacing.md,
}

const $field: ViewStyle = {
  marginBottom: spacing.sm,
}

const $note: TextStyle = {
  fontSize: 14,
  color: "#666",
  fontStyle: "italic",
  marginVertical: spacing.md,
  textAlign: "center",
}

const $button: ViewStyle = {
  marginTop: spacing.md,
}
