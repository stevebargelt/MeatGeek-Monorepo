import { FC, useEffect, useState } from "react"
import { View, Text, TouchableOpacity, FlatList, RefreshControl, ScrollView } from "react-native"
import { useNavigation } from "@react-navigation/native"
import type { AppStackScreenProps } from "@/navigators/AppNavigator"
import { useSession } from "@/context/SessionContext"

interface CookHistoryScreenProps extends AppStackScreenProps<"CookHistory"> {}

export const CookHistoryScreen: FC<CookHistoryScreenProps> = () => {
  const navigation = useNavigation()
  const { sessions, isLoading, loadSessions, error, retryLastAction } = useSession()
  const [refreshing, setRefreshing] = useState(false)

  // Load sessions when screen mounts
  useEffect(() => {
    loadSessions()
  }, [])

  const completedSessions = sessions.filter((session) => session.endTime)

  const onRefresh = async () => {
    setRefreshing(true)
    try {
      await loadSessions()
    } finally {
      setRefreshing(false)
    }
  }

  const renderSessionItem = ({ item }: { item: any }) => (
    <View style={{
      backgroundColor: '#f8f8f8',
      padding: 16,
      marginBottom: 12,
      borderRadius: 8,
      borderWidth: 1,
      borderColor: '#e0e0e0'
    }}>
      <View style={{ flexDirection: 'row', justifyContent: 'space-between', marginBottom: 8 }}>
        <Text style={{ fontSize: 18, fontWeight: 'bold' }}>
          {item.title || "Unnamed Cook"}
        </Text>
        <Text style={{ color: '#666', fontSize: 14 }}>
          {item.endTime ? new Date(item.endTime).toLocaleDateString() : "Active"}
        </Text>
      </View>
      
      <View style={{ flexDirection: 'row', alignItems: 'center' }}>
        <View style={{
          width: 8,
          height: 8,
          borderRadius: 4,
          backgroundColor: item.endTime ? '#4CAF50' : '#FF9800',
          marginRight: 8
        }} />
        <Text style={{ color: '#666', fontSize: 14 }}>
          {item.endTime ? "Completed" : "In Progress"}
        </Text>
      </View>
    </View>
  )

  const renderEmptyState = () => (
    <View style={{ alignItems: 'center', justifyContent: 'center', flex: 1, padding: 40 }}>
      <Text style={{ fontSize: 18, fontWeight: 'bold', marginBottom: 10 }}>
        No Cook History
      </Text>
      <Text style={{ textAlign: 'center', color: '#666', marginBottom: 20 }}>
        Your completed cooking sessions will appear here.
      </Text>
      <Text style={{ textAlign: 'center', color: '#666', fontSize: 12 }}>
        Start a new cook session to begin tracking your BBQ adventures!
      </Text>
    </View>
  )

  const renderErrorState = () => (
    <View style={{ alignItems: 'center', justifyContent: 'center', flex: 1, padding: 40 }}>
      <Text style={{ fontSize: 18, fontWeight: 'bold', marginBottom: 10 }}>
        Unable to Load History
      </Text>
      <Text style={{ textAlign: 'center', color: '#666', marginBottom: 20 }}>
        {error || "Check your connection and try again."}
      </Text>
      <TouchableOpacity 
        style={{
          backgroundColor: '#FF9800',
          padding: 12,
          borderRadius: 8
        }}
        onPress={retryLastAction}
      >
        <Text style={{ color: 'white', fontSize: 16 }}>Retry</Text>
      </TouchableOpacity>
    </View>
  )

  return (
    <View style={{ flex: 1, backgroundColor: '#ffffff' }}>
      {/* Header */}
      <View style={{ 
        flexDirection: 'row', 
        alignItems: 'center', 
        padding: 16,
        paddingTop: 50, // Account for status bar
        borderBottomWidth: 1,
        borderBottomColor: '#e0e0e0'
      }}>
        <TouchableOpacity 
          style={{ marginRight: 16 }}
          onPress={() => navigation.goBack()}
        >
          <Text style={{ fontSize: 16, color: '#007AFF' }}>← Back</Text>
        </TouchableOpacity>
        <Text style={{ fontSize: 20, fontWeight: 'bold' }}>Cook History</Text>
      </View>


      {/* Content */}
      <FlatList
        data={completedSessions} // Show only completed sessions
        renderItem={renderSessionItem}
        keyExtractor={(item) => item.id}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
        ListEmptyComponent={error ? renderErrorState : renderEmptyState}
        contentContainerStyle={completedSessions.length === 0 ? { flex: 1 } : { padding: 16 }}
        showsVerticalScrollIndicator={false}
      />
    </View>
  )
}