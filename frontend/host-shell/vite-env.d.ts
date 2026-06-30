/// <reference types="vite/client" />

declare module "identity/*";
declare module "mentoring/*";
declare module "content/*";
declare module "engagement/*";

interface ImportMetaEnv {
  readonly VITE_API_URL: string;
  readonly VITE_COGNITO_USER_POOL_ID: string;
  readonly VITE_COGNITO_CLIENT_ID: string;
  readonly VITE_APPSYNC_URL: string;
  readonly VITE_ENVIRONMENT: string;
  readonly VITE_REMOTE_BASE_URL: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
