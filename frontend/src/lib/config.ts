/**
 * Every environment-specific value the app needs, read once at startup. Values come
 * from Vite's `import.meta.env`, which in production is populated by the environment
 * variables the CDK stack sets on the Amplify app (see VerificationEngineStack.Frontend.cs)
 * and locally from `.env.local` (see .env.example).
 */
function requireEnv(name: string, value: string | undefined): string {
  if (!value) {
    throw new Error(
      `Missing required environment variable ${name}. Copy .env.example to .env.local ` +
        "and fill in the values from your `cdk deploy` output.",
    );
  }
  return value;
}

export const config = {
  apiBaseUrl: requireEnv("VITE_API_BASE_URL", import.meta.env.VITE_API_BASE_URL),
  cognitoUserPoolId: requireEnv("VITE_COGNITO_USER_POOL_ID", import.meta.env.VITE_COGNITO_USER_POOL_ID),
  cognitoClientId: requireEnv("VITE_COGNITO_CLIENT_ID", import.meta.env.VITE_COGNITO_CLIENT_ID),
  awsRegion: requireEnv("VITE_AWS_REGION", import.meta.env.VITE_AWS_REGION),
};
