
export interface Employee {
  id: string;
  name: string;
  email: string;
  countryCode: string;
}

export interface Shift {
  id: string;
  employeeId: string;
  employeeName: string;
  date: string;
  startTime: string;
  endTime: string;
  isHoliday: boolean | null;
  holidayName: string | null;
}

export interface ShiftRequest {
  employeeId: string;
  date: string;
  startTime: string;
  endTime: string;
}

export interface ImportRowResult {
  rowNumber: number;
  email: string;
  success: boolean;
  message: string;
}

export interface ImportReport {
  totalRows: number;
  importedRows: number;
  failedRows: number;
  results: ImportRowResult[];
}
