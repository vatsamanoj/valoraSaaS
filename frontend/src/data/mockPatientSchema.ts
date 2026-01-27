import { ModuleSchema } from '../types/schema';

export const mockPatientSchema: ModuleSchema = {
  tenantId: "tenant-001",
  module: "Patient",
  version: 1,
  fields: {
    FirstName: {
      required: true,
      maxLength: 50,
      ui: {
        type: "text",
        label: "First Name",
        placeholder: "John"
      }
    },
    LastName: {
      required: true,
      maxLength: 50,
      ui: {
        type: "text",
        label: "Last Name",
        placeholder: "Doe"
      }
    },
    DateOfBirth: {
      required: true,
      ui: {
        type: "date",
        label: "Date of Birth"
      }
    },
    Gender: {
      required: true,
      ui: {
        type: "select",
        label: "Gender",
        options: ["Male", "Female", "Other"]
      }
    },
    Email: {
      required: false,
      maxLength: 100,
      ui: {
        type: "email",
        label: "Email Address",
        placeholder: "john.doe@example.com"
      }
    },
    Phone: {
      required: true,
      maxLength: 15,
      ui: {
        type: "tel",
        label: "Phone Number",
        placeholder: "+1 (555) 000-0000"
      }
    },
    Notes: {
      required: false,
      maxLength: 500,
      ui: {
        type: "textarea",
        label: "Medical Notes",
        placeholder: "Enter any relevant medical history..."
      }
    }
  }
};
