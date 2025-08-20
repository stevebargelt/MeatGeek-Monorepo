import type { SessionSummary, SessionDetails, SessionStatusDocument } from "./types"

/**
 * Mock data for testing the app without API connectivity
 */
export const mockSessionSummaries: SessionSummary[] = [
  {
    id: "session-1",
    smokerId: "meatgeek3",
    type: "session",
    title: "Memorial Day Brisket",
    endTime: undefined, // Active session
  },
  {
    id: "session-2",
    smokerId: "meatgeek3",
    type: "session",
    title: "Weekend Ribs",
    endTime: new Date(Date.now() - 42 * 60 * 60 * 1000).toISOString(),
  },
  {
    id: "session-3",
    smokerId: "meatgeek3",
    type: "session",
    title: "Test Chicken Thighs",
    endTime: new Date(Date.now() - 94 * 60 * 60 * 1000).toISOString(),
  },
]

export const mockSessionDetails: SessionDetails[] = [
  {
    id: "session-1",
    smokerId: "meatgeek3",
    type: "session",
    title: "Memorial Day Brisket",
    description: "Long cook brisket with point and flat monitoring",
    startTime: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString(), // 4 hours ago
    endTime: undefined, // Active session
    timestamp: new Date().toISOString(),
  },
  {
    id: "session-2",
    smokerId: "meatgeek3",
    type: "session",
    title: "Weekend Ribs",
    description: "Pork ribs with 3-2-1 method",
    startTime: new Date(Date.now() - 48 * 60 * 60 * 1000).toISOString(), // 2 days ago
    endTime: new Date(Date.now() - 42 * 60 * 60 * 1000).toISOString(), // Ended 6 hours later
    timestamp: new Date(Date.now() - 42 * 60 * 60 * 1000).toISOString(),
  },
]

export const mockSessionStatus: SessionStatusDocument = {
  id: "status-1",
  smokerId: "meatgeek3",
  sessionId: "session-1",
  type: "status",
  augerOn: "true",
  blowerOn: "false",
  igniterOn: "false",
  temps: {
    grillTemp: "225",
    probe1Temp: "185",
    probe2Temp: "178",
    probe3Temp: "0",
    probe4Temp: "0",
  },
  fireHealthy: "true",
  mode: "Smoke",
  setPoint: "225",
  modeTime: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString(),
  currentTime: new Date().toISOString(),
  _etag: "mock-etag",
}

/**
 * Simulates API delay
 */
export const delay = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms))

/**
 * Mock API responses for development/testing
 */
export const mockApiResponses = {
  getSessions: async () => {
    await delay(1000) // Simulate network delay
    return { kind: "ok" as const, sessions: mockSessionSummaries }
  },

  createSession: async (sessionData: any) => {
    await delay(1500) // Simulate network delay

    const newSession: SessionDetails = {
      id: `session-${Date.now()}`,
      smokerId: sessionData.smokerId || "meatgeek3",
      type: "session",
      title: sessionData.title,
      description: sessionData.description,
      startTime: new Date().toISOString(),
      endTime: undefined,
      timestamp: new Date().toISOString(),
    }

    return { kind: "ok" as const, session: newSession }
  },

  endSession: async () => {
    await delay(800) // Simulate network delay
    return { kind: "ok" as const }
  },

  getSessionById: async (smokerId: string, sessionId: string) => {
    await delay(600) // Simulate network delay
    const session = mockSessionDetails.find((s) => s.id === sessionId)
    if (session) {
      return { kind: "ok" as const, session }
    }
    return { kind: "not-found" as const }
  },

  getSessionStatus: async (smokerId: string, sessionId: string) => {
    await delay(500) // Simulate network delay

    // Simulate slightly changing temperatures
    const baseGrillTemp = 225 + (Math.random() - 0.5) * 10 // ±5 degrees variation
    const mockStatus: SessionStatusDocument = {
      ...mockSessionStatus,
      sessionId,
      temps: {
        grillTemp: Math.round(baseGrillTemp).toString(),
        probe1Temp: Math.round(185 + Math.random() * 5).toString(), // Slowly rising
        probe2Temp: Math.round(178 + Math.random() * 4).toString(), // Slowly rising
        probe3Temp: "0",
        probe4Temp: "0",
      },
      currentTime: new Date().toISOString(),
    }

    return { kind: "ok" as const, status: mockStatus }
  },
}
