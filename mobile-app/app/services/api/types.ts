/**
 * These types indicate the shape of the data you expect to receive from your
 * API endpoint, assuming it's a JSON object like we have.
 */
export interface EpisodeItem {
  title: string
  pubDate: string
  link: string
  guid: string
  author: string
  thumbnail: string
  description: string
  content: string
  enclosure: {
    link: string
    type: string
    length: number
    duration: number
    rating: { scheme: string; value: string }
  }
  categories: string[]
}

export interface ApiFeedResponse {
  status: string
  feed: {
    url: string
    title: string
    link: string
    author: string
    description: string
    image: string
  }
  items: EpisodeItem[]
}

/**
 * The options used to configure apisauce.
 */
export interface ApiConfig {
  /**
   * The URL of the api.
   */
  url: string

  /**
   * Milliseconds before we timeout the request.
   */
  timeout: number
}

/**
 * MeatGeek Sessions API Types - Matching actual backend models
 */
export interface StatusTemps {
  grillTemp: string
  probe1Temp: string
  probe2Temp: string
  probe3Temp: string
  probe4Temp: string
}

export interface SessionStatusDocument {
  id: string
  smokerId: string
  sessionId: string
  type: "status"
  augerOn: string
  blowerOn: string
  igniterOn: string
  temps: StatusTemps
  fireHealthy: string
  mode: string
  setPoint: string
  modeTime: string // ISO date string
  currentTime: string // ISO date string
  _etag: string
}

export interface SessionSummary {
  id: string
  smokerId: string
  type: "session"
  title: string
  endTime?: string // ISO date string
}

export interface SessionDetails {
  Id: string // API returns capitalized field names
  SmokerId: string
  Type: "session"
  Title: string
  Description: string
  StartTime?: string // ISO date string
  EndTime?: string // ISO date string
  TimeStamp: string // ISO date string
}

export interface CreateSessionRequest {
  smokerId: string
  title: string
  description: string
  startTime?: string // ISO date string
}

// Response types
export type SessionSummaries = SessionSummary[]
