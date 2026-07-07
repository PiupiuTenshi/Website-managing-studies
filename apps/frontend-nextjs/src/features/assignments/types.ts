export interface Assignment {
  id: string;
  subjectId: string;
  title: string;
  description: string | null;
  contentJson: any | null;
  deadlineAt: string | null;
  allowLateSubmission: boolean;
  maxAttempts: number | null;
  status: "Draft" | "Published" | "Archived";
  createdBy: string;
  createdByName: string;
  createdAt: string;
  updatedAt: string;
}

export interface AssignmentTarget {
  id: string;
  assignmentId: string;
  targetType: "ClassRoom" | "Student";
  targetId: string;
  targetName: string;
}

export interface CreateAssignmentRequest {
  subjectId: string;
  title: string;
  description?: string | null;
  contentJson?: any | null;
  deadlineAt?: string | null;
  allowLateSubmission: boolean;
  maxAttempts?: number | null;
}

export interface UpdateAssignmentRequest extends CreateAssignmentRequest {}

export interface AssignmentTargetInput {
  targetType: "ClassRoom" | "Student";
  targetId: string;
}

export interface AssignTargetRequest {
  targets: AssignmentTargetInput[];
}
