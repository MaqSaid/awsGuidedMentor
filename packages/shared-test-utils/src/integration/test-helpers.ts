import type { APIGatewayProxyEvent, APIGatewayProxyResult, Context } from 'aws-lambda';

/**
 * Creates a minimal API Gateway proxy event for testing Lambda handlers.
 */
export function createApiGatewayEvent(overrides: Partial<APIGatewayProxyEvent> = {}): APIGatewayProxyEvent {
  return {
    httpMethod: 'POST',
    path: '/',
    pathParameters: null,
    queryStringParameters: null,
    multiValueQueryStringParameters: null,
    headers: {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer test-token',
    },
    multiValueHeaders: {},
    body: null,
    isBase64Encoded: false,
    resource: '',
    stageVariables: null,
    requestContext: {
      accountId: '123456789012',
      apiId: 'test-api',
      authorizer: {
        claims: {
          sub: 'test-user-id',
          email: 'test@example.com',
        },
      },
      httpMethod: 'POST',
      identity: {
        accessKey: null,
        accountId: null,
        apiKey: null,
        apiKeyId: null,
        caller: null,
        clientCert: null,
        cognitoAuthenticationProvider: null,
        cognitoAuthenticationType: null,
        cognitoIdentityId: null,
        cognitoIdentityPoolId: null,
        principalOrgId: null,
        sourceIp: '127.0.0.1',
        user: null,
        userAgent: 'test-agent',
        userArn: null,
      },
      path: '/',
      protocol: 'HTTP/1.1',
      requestId: 'test-request-id',
      requestTimeEpoch: Date.now(),
      resourceId: 'test-resource',
      resourcePath: '/',
      stage: 'test',
    },
    ...overrides,
  };
}

/**
 * Creates a minimal Lambda context for testing.
 */
export function createLambdaContext(overrides: Partial<Context> = {}): Context {
  return {
    callbackWaitsForEmptyEventLoop: true,
    functionName: 'test-function',
    functionVersion: '$LATEST',
    invokedFunctionArn: 'arn:aws:lambda:us-east-1:123456789012:function:test-function',
    memoryLimitInMB: '256',
    awsRequestId: 'test-request-id',
    logGroupName: '/aws/lambda/test-function',
    logStreamName: '2024/01/01/[$LATEST]test-stream',
    getRemainingTimeInMillis: () => 30000,
    done: () => {},
    fail: () => {},
    succeed: () => {},
    ...overrides,
  };
}

/**
 * Parses a Lambda handler response body from JSON.
 */
export function parseResponseBody<T = unknown>(result: APIGatewayProxyResult): T {
  if (!result.body) {
    throw new Error('Response body is empty');
  }
  return JSON.parse(result.body) as T;
}

/**
 * Creates an authenticated API Gateway event with a specific user ID in the authorizer claims.
 */
export function createAuthenticatedEvent(
  userId: string,
  body?: object,
  overrides: Partial<APIGatewayProxyEvent> = {},
): APIGatewayProxyEvent {
  return createApiGatewayEvent({
    body: body ? JSON.stringify(body) : null,
    requestContext: {
      ...createApiGatewayEvent().requestContext,
      authorizer: {
        claims: {
          sub: userId,
          email: `${userId}@example.com`,
        },
      },
    },
    ...overrides,
  });
}
