export interface StudentAssignment {
  assignmentId: string;
  title: string;
  description: string | null;
  deadlineAt: string | null;
  allowLateSubmission: boolean;
  status: string;
  createdByName: string;
  createdAt: string;
  submissionId: string | null;
  submissionStatus: string | null;
  submittedAt: string | null;
  gradeScore: number | null;
}

export interface Submission {
  id: string;
  assignmentId: string;
  studentId: string;
  contentJson: any | null;
  status: string;
  submittedAt: string | null;
  gradedAt: string | null;
  gradeScore: number | null;
  feedbackJson: any | null;
  createdAt: string;
  updatedAt: string;
}

export interface DraftSubmissionRequest {
  contentJson?: any | null;
}

export interface SubmitAssignmentRequest {
  contentJson?: any | null;
}

export interface ManagerSubmission {
  id: string;
  assignmentId: string;
  studentId: string;
  studentName: string;
  status: string;
  submittedAt: string | null;
  gradedAt: string | null;
  gradeScore: number | null;
  createdAt: string;
}

export interface GradeSubmissionRequest {
  gradeScore: number;
  feedbackJson?: any | null;
}
