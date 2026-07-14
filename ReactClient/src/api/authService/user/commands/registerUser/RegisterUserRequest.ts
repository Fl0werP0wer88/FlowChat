export interface RegisterUserRequest {
  id: string;
  email: string;
  friendlyUserId: string;
  password: string;
  firstName?: string;
  lastName?: string;
  organization?: string;
}
