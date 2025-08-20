import { FC } from "react"
import { View } from "react-native"
import type { AppStackScreenProps } from "@/navigators/AppNavigator"
import { Screen } from "@/components/Screen"
import { Text } from "@/components/Text"

interface CookHistoryScreenProps extends AppStackScreenProps<"CookHistory"> {}

export const CookHistoryScreen: FC<CookHistoryScreenProps> = () => {
  console.log("=== SIMPLE CookHistoryScreen RENDERING ===")
  
  return (
    <Screen preset="fixed">
      <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center', padding: 20 }}>
        <Text preset="heading" text="Cook History" />
        <Text text="This is a simplified version to test rendering" />
        <Text text="If you can see this, the screen component works!" />
      </View>
    </Screen>
  )
}