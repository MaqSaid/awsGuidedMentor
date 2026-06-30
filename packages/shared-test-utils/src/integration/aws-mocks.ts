import { mockClient } from 'aws-sdk-client-mock';
import { DynamoDBClient } from '@aws-sdk/client-dynamodb';
import { S3Client } from '@aws-sdk/client-s3';
import { CognitoIdentityProviderClient } from '@aws-sdk/client-cognito-identity-provider';
import { BedrockRuntimeClient } from '@aws-sdk/client-bedrock-runtime';

/**
 * Creates a mocked DynamoDB client for integration testing.
 * Use `.on(Command).resolves(response)` to stub specific operations.
 *
 * @example
 * ```ts
 * const ddbMock = createDynamoDBMock();
 * ddbMock.on(GetItemCommand).resolves({ Item: mockItem });
 * // ... run your handler
 * ddbMock.restore();
 * ```
 */
export function createDynamoDBMock() {
  return mockClient(DynamoDBClient);
}

/**
 * Creates a mocked S3 client for integration testing.
 *
 * @example
 * ```ts
 * const s3Mock = createS3Mock();
 * s3Mock.on(PutObjectCommand).resolves({});
 * // ... run your handler
 * s3Mock.restore();
 * ```
 */
export function createS3Mock() {
  return mockClient(S3Client);
}

/**
 * Creates a mocked Cognito client for integration testing.
 *
 * @example
 * ```ts
 * const cognitoMock = createCognitoMock();
 * cognitoMock.on(SignUpCommand).resolves({ UserSub: 'user-123' });
 * // ... run your handler
 * cognitoMock.restore();
 * ```
 */
export function createCognitoMock() {
  return mockClient(CognitoIdentityProviderClient);
}

/**
 * Creates a mocked Bedrock Runtime client for integration testing.
 *
 * @example
 * ```ts
 * const bedrockMock = createBedrockMock();
 * bedrockMock.on(ConverseCommand).resolves({ output: { message: { content: [...] } } });
 * // ... run your handler
 * bedrockMock.restore();
 * ```
 */
export function createBedrockMock() {
  return mockClient(BedrockRuntimeClient);
}

/**
 * Restores all AWS SDK mocks. Call this in afterEach/afterAll hooks.
 */
export function restoreAllMocks(...mocks: ReturnType<typeof mockClient>[]) {
  mocks.forEach(mock => mock.restore());
}
