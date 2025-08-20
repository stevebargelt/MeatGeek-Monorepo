/**
 * This Api class lets you define an API endpoint and methods to request
 * data and process it.
 *
 * See the [Backend API Integration](https://docs.infinite.red/ignite-cli/boilerplate/app/services/#backend-api-integration)
 * documentation for more details.
 */
import { ApiResponse, ApisauceInstance, create } from "apisauce"

import Config from "@/config"
import type { EpisodeItem } from "@/services/api/types"

import { GeneralApiProblem, getGeneralApiProblem } from "./apiProblem"
import type {
  ApiConfig,
  ApiFeedResponse,
  SessionSummaries,
  SessionDetails,
  CreateSessionRequest,
  SessionStatusDocument,
} from "./types"

/**
 * Configuring the apisauce instance.
 */
export const DEFAULT_API_CONFIG: ApiConfig = {
  url: Config.API_URL,
  timeout: 10000,
}

/**
 * Manages all requests to the API. You can use this class to build out
 * various requests that you need to call from your backend API.
 */
export class Api {
  apisauce: ApisauceInstance
  config: ApiConfig

  /**
   * Set up our API instance. Keep this lightweight!
   */
  constructor(config: ApiConfig = DEFAULT_API_CONFIG) {
    this.config = config
    this.apisauce = create({
      baseURL: this.config.url,
      timeout: this.config.timeout,
      headers: {
        Accept: "application/json",
      },
    })
  }

  /**
   * Gets a list of recent React Native Radio episodes.
   */
  async getEpisodes(): Promise<{ kind: "ok"; episodes: EpisodeItem[] } | GeneralApiProblem> {
    // make the api call
    const response: ApiResponse<ApiFeedResponse> = await this.apisauce.get(
      `api.json?rss_url=https%3A%2F%2Ffeeds.simplecast.com%2FhEI_f9Dx`,
    )

    // the typical ways to die when calling an api
    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    // transform the data into the format we are expecting
    try {
      const rawData = response.data

      // This is where we transform the data into the shape we expect for our model.
      const episodes: EpisodeItem[] =
        rawData?.items.map((raw) => ({
          ...raw,
        })) ?? []

      return { kind: "ok", episodes }
    } catch (e) {
      if (__DEV__ && e instanceof Error) {
        console.error(`Bad data: ${e.message}\n${response.data}`, e.stack)
      }
      return { kind: "bad-data" }
    }
  }

  /**
   * Gets all sessions for a smoker
   */
  async getSessions(
    smokerId: string,
  ): Promise<{ kind: "ok"; sessions: SessionSummaries } | GeneralApiProblem> {
    console.log(`Making API call to: ${this.config.url}/api/sessions/${smokerId}`)

    const response: ApiResponse<Record<string, SessionSummary>> = await this.apisauce.get(
      `/api/sessions/${smokerId}`,
    )

    console.log("API Response status:", response.status)
    console.log("API Response ok:", response.ok)
    console.log("API Response data:", response.data)
    console.log("API Response problem:", response.problem)

    if (!response.ok) {
      console.log("API call failed, getting problem...")
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    try {
      // Transform the key-value object into an array
      const sessionsObject = response.data ?? {}
      const sessions: SessionSummaries = Object.entries(sessionsObject).map(([id, session]) => ({
        ...session,
        id, // Use the key as the id
      }))

      console.log("Transformed sessions array:", sessions)
      return { kind: "ok", sessions }
    } catch (e) {
      if (__DEV__ && e instanceof Error) {
        console.error(`Bad sessions data: ${e.message}\n${response.data}`, e.stack)
      }
      return { kind: "bad-data" }
    }
  }

  /**
   * Creates a new cooking session
   */
  async createSession(
    smokerId: string,
    sessionData: CreateSessionRequest,
  ): Promise<{ kind: "ok"; session: SessionDetails } | GeneralApiProblem> {
    const response: ApiResponse<SessionDetails> = await this.apisauce.post(
      `/api/sessions/${smokerId}`,
      sessionData,
    )

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    try {
      const session = response.data
      if (!session) return { kind: "bad-data" }
      return { kind: "ok", session }
    } catch (e) {
      if (__DEV__ && e instanceof Error) {
        console.error(`Bad session data: ${e.message}\n${response.data}`, e.stack)
      }
      return { kind: "bad-data" }
    }
  }

  /**
   * Ends an active cooking session (PATCH /api/endsession/{smokerId}/{sessionId})
   */
  async endSession(
    smokerId: string,
    sessionId: string,
  ): Promise<{ kind: "ok" } | GeneralApiProblem> {
    console.log(`Ending session: PATCH /api/endsession/${smokerId}/${sessionId}`)

    const response: ApiResponse<any> = await this.apisauce.patch(
      `/api/endsession/${smokerId}/${sessionId}`,
      {
        endTime: new Date().toISOString(),
      },
    )

    console.log("EndSession response:", response.status, response.problem)

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    return { kind: "ok" }
  }

  /**
   * Gets session details by ID
   */
  async getSessionById(
    smokerId: string,
    sessionId: string,
  ): Promise<{ kind: "ok"; session: SessionDetails } | GeneralApiProblem> {
    const response: ApiResponse<SessionDetails> = await this.apisauce.get(
      `/api/sessions/${smokerId}/${sessionId}`,
    )

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    try {
      const session = response.data
      if (!session) return { kind: "bad-data" }
      return { kind: "ok", session }
    } catch (e) {
      if (__DEV__ && e instanceof Error) {
        console.error(`Bad session data: ${e.message}\n${response.data}`, e.stack)
      }
      return { kind: "bad-data" }
    }
  }

  /**
   * Gets current session status/temperatures
   */
  async getSessionStatus(
    smokerId: string,
    sessionId: string,
  ): Promise<
    { kind: "ok"; status: SessionStatusDocument } | { kind: "not-found" } | GeneralApiProblem
  > {
    console.log(`Getting session status: /api/sessions/statuses/${smokerId}/${sessionId}`)

    const response: ApiResponse<SessionStatusDocument | any[]> = await this.apisauce.get(
      `/api/sessions/statuses/${smokerId}/${sessionId}`,
    )

    console.log("Status API response:", response.status, response.data)

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    try {
      const data = response.data

      // Handle case where API returns empty array (no status data)
      if (Array.isArray(data) && data.length === 0) {
        console.log("No status data available for this session")
        return { kind: "not-found" }
      }

      // Handle case where we get actual status data
      if (data && typeof data === "object" && !Array.isArray(data)) {
        return { kind: "ok", status: data as SessionStatusDocument }
      }

      return { kind: "bad-data" }
    } catch (e) {
      if (__DEV__ && e instanceof Error) {
        console.error(`Bad status data: ${e.message}\n${response.data}`, e.stack)
      }
      return { kind: "bad-data" }
    }
  }
}

// Singleton instance of the API for convenience
export const api = new Api()
