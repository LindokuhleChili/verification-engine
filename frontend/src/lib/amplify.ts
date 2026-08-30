import { Amplify } from "aws-amplify";
import { config } from "./config";

/**
 * Configures the Amplify Auth category to talk to this project's Cognito User Pool.
 * Only Auth is configured - no Storage/API categories - because this app talks to its
 * own API Gateway directly (see lib/api.ts) rather than through Amplify's API category.
 */
Amplify.configure({
  Auth: {
    Cognito: {
      userPoolId: config.cognitoUserPoolId,
      userPoolClientId: config.cognitoClientId,
      signUpVerificationMethod: "code",
    },
  },
});
