export interface ConfigBaseProps {
  persistNavigation: "always" | "dev" | "prod" | "never"
  catchErrors: "always" | "dev" | "prod" | "never"
  exitRoutes: string[]
  API_URL: string
  SMOKER_ID: string
}

export type PersistNavigationConfig = ConfigBaseProps["persistNavigation"]

const BaseConfig: ConfigBaseProps = {
  // This feature is particularly useful in development mode, but
  // can be used in production as well if you prefer.
  persistNavigation: "dev",

  /**
   * Only enable if we're catching errors in the right environment
   */
  catchErrors: "always",

  /**
   * This is a list of all the route names that will exit the app if the back button
   * is pressed while in that screen. Only affects Android.
   */
  exitRoutes: ["Welcome", "ActiveCook"],

  /**
   * MeatGeek API Configuration
   */
  API_URL: "https://meatgeeksessionsapi.azurewebsites.net",
  SMOKER_ID: "test-smoker-1", // This will be overridden in dev/prod configs
}

export default BaseConfig
