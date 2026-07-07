export type HealthResponse = {
  status: string;
  service: string;
  environment: string;
  databaseConfigured: boolean;
  checkedAt: string;
};
