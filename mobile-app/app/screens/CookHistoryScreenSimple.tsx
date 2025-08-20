import { FC } from "react"
import { View, Text, TouchableOpacity } from "react-native"
import { useNavigation } from "@react-navigation/native"
import type { AppStackScreenProps } from "@/navigators/AppNavigator"

interface CookHistoryScreenProps extends AppStackScreenProps<"CookHistory"> {}

export const CookHistoryScreen: FC<CookHistoryScreenProps> = () => {
  const navigation = useNavigation()
  
  return (
    <View style={{ 
      flex: 1, 
      justifyContent: 'center', 
      alignItems: 'center', 
      padding: 20,
      backgroundColor: '#ffffff'
    }}>
      <Text style={{ fontSize: 24, fontWeight: 'bold', marginBottom: 20 }}>
        Cook History Screen
      </Text>
      <Text style={{ fontSize: 16, marginBottom: 10 }}>
        This is a basic React Native View
      </Text>
      <Text style={{ fontSize: 16, marginBottom: 10 }}>
        If you can see this, the component renders!
      </Text>
      <Text style={{ fontSize: 16, color: 'green', marginBottom: 30 }}>
        Navigation SUCCESS!
      </Text>
      
      <TouchableOpacity 
        style={{
          backgroundColor: '#007AFF',
          padding: 15,
          borderRadius: 8
        }}
        onPress={() => navigation.goBack()}
      >
        <Text style={{ color: 'white', fontSize: 16 }}>
          Go Back to Active Cook
        </Text>
      </TouchableOpacity>
    </View>
  )
}