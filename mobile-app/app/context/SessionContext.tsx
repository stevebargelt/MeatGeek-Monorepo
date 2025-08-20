import React, { createContext, useContext, useState, useEffect, ReactNode, useRef } from "react"
import { api } from "@/services/api"
import Config from "@/config"
import type {
  SessionSummary,
  SessionDetails,
  SessionStatusDocument,
  CreateSessionRequest,
} from "@/services/api/types"
import { storage } from "@/utils/storage"
import { mockApiResponses } from "@/services/api/mockData"
import type { ProbeDisplay } from "@/components/TemperatureCard"

interface SessionContextType {
  // State
  sessions: SessionSummary[]
  activeSession: SessionDetails | null
  currentStatus: SessionStatusDocument | null
  probeDisplays: ProbeDisplay[]
  isLoading: boolean
  isConnected: boolean
  lastRefresh: Date | null
  error: string | null
  usesCelsius: boolean
  temperaturePresets: TemperaturePreset[]

  // Actions
  loadSessions: () => Promise<void>
  startSession: (sessionData: CreateSessionRequest) => Promise<boolean>
  endActiveSession: () => Promise<boolean>
  loadSessionStatus: (sessionId: string) => Promise<void>
  toggleTemperatureUnit: () => void
  retryLastAction: () => Promise<void>
  clearError: () => void
}

interface TemperaturePreset {
  name: string
  temperature: number
}

const defaultPresets: TemperaturePreset[] = [
  { name: "Low & Slow", temperature: 225 },
  { name: "Hot & Fast", temperature: 275 },
  { name: "Chicken", temperature: 350 },
]

const SessionContext = createContext<SessionContextType | undefined>(undefined)

export const useSession = () => {
  const context = useContext(SessionContext)
  if (!context) {
    throw new Error("useSession must be used within a SessionProvider")
  }
  return context
}

interface SessionProviderProps {
  children: ReactNode
}

export const SessionProvider: React.FC<SessionProviderProps> = ({ children }) => {
  const [sessions, setSessions] = useState<SessionSummary[]>([])
  const [activeSession, setActiveSession] = useState<SessionDetails | null>(null)
  const [currentStatus, setCurrentStatus] = useState<SessionStatusDocument | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [isLoadingDetails, setIsLoadingDetails] = useState(false) // Prevent concurrent detail loading
  const [isConnected, setIsConnected] = useState(true)
  const [lastRefresh, setLastRefresh] = useState<Date | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [usesCelsius, setUsesCelsius] = useState(false)
  const [temperaturePresets] = useState<TemperaturePreset[]>(defaultPresets)
  const [useMockData] = useState(true) // Temporarily use mock data for debugging
  const lastLoadedSessionRef = useRef<string | null>(null) // Track last loaded session to prevent loops

  // Initialize from storage
  useEffect(() => {
    hydrate()
  }, [])

  const hydrate = () => {
    // Load persisted sessions
    const persistedSessions = storage.getString("sessions")
    if (persistedSessions) {
      try {
        const sessionData = JSON.parse(persistedSessions)
        setSessions(sessionData)
      } catch (e) {
        console.warn("Failed to hydrate sessions:", e)
      }
    }

    // Load temperature preference
    const tempUnit = storage.getString("temperatureUnit")
    if (tempUnit === "celsius") {
      setUsesCelsius(true)
    }
  }

  // Convert StatusTemps to ProbeDisplay array for UI
  const convertStatusToProbeDisplays = (status: SessionStatusDocument): ProbeDisplay[] => {
    console.log("convertStatusToProbeDisplays called with:", status)

    // Add comprehensive null checks
    if (!status) {
      console.warn("Status is null/undefined, returning empty probe displays")
      return []
    }

    if (!status.temps) {
      console.warn("Status.temps is undefined, returning empty probe displays")
      return []
    }

    try {
      return [
        {
          probeName: "Grill",
          currentTemp: parseFloat(status.temps.grillTemp || "0") || null,
          targetTemp: parseFloat(status.setPoint || "0") || null,
          isActive: true,
        },
        {
          probeName: "Probe 1",
          currentTemp: parseFloat(status.temps.probe1Temp || "0") || null,
          targetTemp: 203, // Default target for meat probes
          isActive: parseFloat(status.temps.probe1Temp || "0") > 0,
        },
        {
          probeName: "Probe 2",
          currentTemp: parseFloat(status.temps.probe2Temp || "0") || null,
          targetTemp: 203,
          isActive: parseFloat(status.temps.probe2Temp || "0") > 0,
        },
        {
          probeName: "Probe 3",
          currentTemp: parseFloat(status.temps.probe3Temp || "0") || null,
          targetTemp: 203,
          isActive: parseFloat(status.temps.probe3Temp || "0") > 0,
        },
        {
          probeName: "Probe 4",
          currentTemp: parseFloat(status.temps.probe4Temp || "0") || null,
          targetTemp: 203,
          isActive: parseFloat(status.temps.probe4Temp || "0") > 0,
        },
      ]
    } catch (error) {
      console.error("Error in convertStatusToProbeDisplays:", error)
      return []
    }
  }

  const probeDisplays: ProbeDisplay[] =
    currentStatus && currentStatus.temps ? convertStatusToProbeDisplays(currentStatus) : []

  const loadSessions = async () => {
    if (isLoading) {
      console.log("Already loading sessions, skipping...")
      return
    }

    console.log("Starting loadSessions...")
    setIsLoading(true)
    setError(null)

    try {
      let result

      if (useMockData) {
        console.log("Using mock data for getSessions")
        result = await mockApiResponses.getSessions()
        console.log("Mock result:", result)
      } else {
        console.log("Using real API for getSessions")
        result = await api.getSessions(Config.SMOKER_ID)
      }

      if (result.kind === "ok") {
        console.log("API Response:", result.sessions)
        console.log("Sessions type:", typeof result.sessions)
        console.log("Sessions is array:", Array.isArray(result.sessions))

        // The API returns an array directly, not wrapped in an object
        const sessions = Array.isArray(result.sessions) ? result.sessions : []
        setSessions(sessions)
        setLastRefresh(new Date())
        setIsConnected(true)

        // Find active session (one without endTime)
        const activeSummary = sessions.find((s) => !s.endTime)
        if (
          activeSummary &&
          lastLoadedSessionRef.current !== activeSummary.id &&
          !isLoadingDetails
        ) {
          // Only load details if we haven't loaded this session before
          lastLoadedSessionRef.current = activeSummary.id
          await loadActiveSessionDetails(activeSummary.id)
        } else if (!activeSummary) {
          setActiveSession(null)
          setCurrentStatus(null)
          lastLoadedSessionRef.current = null
        } else if (activeSummary && lastLoadedSessionRef.current === activeSummary.id) {
          console.log(`Skipping session details for ${activeSummary.id} - already loaded`)
        }

        // Persist to storage
        storage.set("sessions", JSON.stringify(sessions))
      } else {
        setError("Failed to load sessions")
        setIsConnected(false)
      }
    } catch (e) {
      console.error("LoadSessions error:", e)
      setError(`Network error: ${e instanceof Error ? e.message : "Unknown error"}`)
      setIsConnected(false)
    } finally {
      setIsLoading(false)
    }
  }

  const loadActiveSessionDetails = async (sessionId: string) => {
    console.log(`Loading active session details for: ${sessionId}`)
    if (isLoadingDetails) {
      console.log("Already loading session details, skipping...")
      return
    }

    setIsLoadingDetails(true)
    try {
      let detailsResult, statusResult

      console.log(`Calling getSessionById(${Config.SMOKER_ID}, ${sessionId})`)
      detailsResult = await api.getSessionById(Config.SMOKER_ID, sessionId)
      console.log("getSessionById result:", detailsResult)

      console.log(`Calling getSessionStatus(${Config.SMOKER_ID}, ${sessionId})`)
      statusResult = await api.getSessionStatus(Config.SMOKER_ID, sessionId)
      console.log("getSessionStatus result:", statusResult)

      if (detailsResult.kind === "ok") {
        console.log("Setting active session:", detailsResult.session)
        setActiveSession(detailsResult.session)
      } else {
        console.warn("Failed to load session details:", detailsResult)
      }

      if (statusResult.kind === "ok") {
        console.log("Setting current status:", statusResult.status)
        setCurrentStatus(statusResult.status)
      } else if (statusResult.kind === "not-found") {
        console.log("No status data available for this session (device not active)")
        setCurrentStatus(null)
      } else {
        console.warn("Failed to load session status:", statusResult)
        setCurrentStatus(null)
      }
    } catch (e) {
      console.error("Exception in loadActiveSessionDetails:", e)
    } finally {
      setIsLoadingDetails(false)
    }
  }

  const startSession = async (sessionData: CreateSessionRequest): Promise<boolean> => {
    setIsLoading(true)
    setError(null)

    try {
      let result

      if (useMockData) {
        result = await mockApiResponses.createSession(sessionData)
      } else {
        result = await api.createSession(Config.SMOKER_ID, sessionData)
      }

      if (result.kind === "ok") {
        // Add the new session to the existing list (avoid recursive loadSessions call)
        setSessions((prev) => [...prev, result.session])
        setActiveSession(result.session)
        setIsConnected(true)
        return true
      } else {
        setError("Failed to start session")
        setIsConnected(false)
        return false
      }
    } catch (e) {
      setError("Network error")
      setIsConnected(false)
      return false
    } finally {
      setIsLoading(false)
    }
  }

  const endActiveSession = async (): Promise<boolean> => {
    if (!activeSession) return false

    setIsLoading(true)
    setError(null)

    try {
      let result

      if (useMockData) {
        result = await mockApiResponses.endSession()
      } else {
        result = await api.endSession(Config.SMOKER_ID, activeSession.Id)
      }

      if (result.kind === "ok") {
        // Update the active session to mark it as completed (avoid recursive loadSessions call)
        setSessions((prev) =>
          prev.map((session) =>
            session.id === activeSession.Id
              ? { ...session, endTime: new Date().toISOString() }
              : session,
          ),
        )
        setActiveSession(null)
        setCurrentStatus(null)
        setIsConnected(true)
        return true
      } else {
        setError("Failed to end session")
        setIsConnected(false)
        return false
      }
    } catch (e) {
      setError("Network error")
      setIsConnected(false)
      return false
    } finally {
      setIsLoading(false)
    }
  }

  const loadSessionStatus = async (sessionId: string) => {
    if (!sessionId) return

    try {
      let result

      if (useMockData) {
        result = await mockApiResponses.getSessionStatus(Config.SMOKER_ID, sessionId)
      } else {
        result = await api.getSessionStatus(Config.SMOKER_ID, sessionId)
      }

      if (result.kind === "ok") {
        setCurrentStatus(result.status)
        setLastRefresh(new Date())
        setIsConnected(true)
      } else if (result.kind === "not-found") {
        console.log("No status data available for this session")
        setCurrentStatus(null)
        setLastRefresh(new Date())
        setIsConnected(true)
      } else {
        setIsConnected(false)
      }
    } catch (e) {
      setIsConnected(false)
    }
  }

  const toggleTemperatureUnit = () => {
    const newValue = !usesCelsius
    setUsesCelsius(newValue)
    storage.set("temperatureUnit", newValue ? "celsius" : "fahrenheit")
  }

  const retryLastAction = async () => {
    await loadSessions()
  }

  const clearError = () => {
    setError(null)
  }

  const value: SessionContextType = {
    sessions,
    activeSession,
    currentStatus,
    probeDisplays,
    isLoading,
    isConnected,
    lastRefresh,
    error,
    usesCelsius,
    temperaturePresets,
    loadSessions,
    startSession,
    endActiveSession,
    loadSessionStatus,
    toggleTemperatureUnit,
    retryLastAction,
    clearError,
  }

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>
}
