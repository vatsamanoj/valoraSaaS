using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Net;

class Program
{
    static async Task Main(string[] args)
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        string connString = "mongodb+srv://vatsamanoj_db_user:cVmX5s193hAEpBd4@cluster0.k6mzgv0.mongodb.net/ValoraReadDb?authSource=admin&retryWrites=true&w=majority&tls=true";
        string dbName = "ValoraReadDb";
        string collectionName = "PlatformObjectTemplate";

        Console.WriteLine("Connecting to MongoDB...");

        try
        {
            var settings = MongoClientSettings.FromConnectionString(connString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);

            var client = new MongoClient(settings);
            var db = client.GetDatabase(dbName);
            var collection = db.GetCollection<BsonDocument>(collectionName);

            var json = """
{
  "_id": "TENANT_LAB_001",
  "tenantId": "LAB_001",
  "tenantName": "ABC Diagnostics",
  "subscription": {
    "planCode": "PRO",
    "status": "ACTIVE",
    "billingCycle": "MONTHLY",
    "validFrom": "2026-01-01",
    "validTo": "2026-12-31",
    "limits": {
      "maxUsers": 50,
      "maxScreens": 200,
      "maxRecordsPerScreen": 100000,
      "storageMB": 10240
    }
  },
  "features": {
    "dynamicScreens": true,
    "versioning": true,
    "previewEnvironment": true,
    "prodEnvironment": true,
    "auditLogs": true,
    "aiAssistant": true,
    "apiAccess": true
  },
  "roles": {
    "PlatformAdmin": {
      "permissions": [
        "*"
      ]
    },
    "TenantAdmin": {
      "permissions": [
        "screen:create",
        "screen:edit",
        "screen:publish",
        "user:manage"
      ]
    },
    "Doctor": {
      "permissions": [
        "screen:view"
      ]
    },
    "Receptionist": {
      "permissions": [
        "screen:view",
        "screen:create"
      ]
    }
  },
  "screenRights": {
    "Patient": {
      "default": {
        "visible": true,
        "actions": [
          "view"
        ]
      },
      "byRole": {
        "Doctor": [
          "view"
        ],
        "Receptionist": [
          "view",
          "create"
        ],
        "TenantAdmin": [
          "view",
          "create",
          "edit",
          "delete"
        ]
      }
    },
    "PathologyBilling": {
      "default": {
        "visible": true,
        "actions": [
          "view"
        ]
      },
      "byRole": {
        "Receptionist": [
          "view",
          "create"
        ],
        "TenantAdmin": [
          "view",
          "create",
          "edit",
          "delete"
        ]
      }
    }
  },
  "environments": {
    "test": {
      "screens": {
        "Nurse": {
          "v2": {
            "objectCode": "Nurse",
            "version": 2,
            "isPublished": true,
            "audit": {
              "tenantId": {
                "type": "text",
                "required": true
              },
              "createdAt": {
                "type": "datetime"
              },
              "createdBy": {
                "type": "text"
              },
              "updatedAt": {
                "type": "datetime"
              },
              "updatedBy": {
                "type": "text"
              },
              "isDeleted": {
                "type": "boolean",
                "default": false
              }
            },
            "fields": {
              "Department": {
                "required": false
              },
              "Name": {
                "required": true,
                "maxLength": 100
              },
              "NurseCode": {
                "required": true,
                "maxLength": 20
              }
            }
          }
        },
        "OrderTest": {
          "v3": {
            "objectCode": "OrderTest",
            "version": 3,
            "isPublished": true,
            "audit": {
              "tenantId": {
                "type": "text",
                "required": true
              },
              "createdAt": {
                "type": "datetime"
              },
              "createdBy": {
                "type": "text"
              },
              "updatedAt": {
                "type": "datetime"
              },
              "updatedBy": {
                "type": "text"
              },
              "isDeleted": {
                "type": "boolean",
                "default": false
              }
            },
            "fields": {
              "Name": {
                "ui": {
                  "type": "text"
                },
                "required": true
              },
              "Amount": {
                "ui": {
                  "type": "number"
                },
                "required": true
              }
            }
          }
        }
      }
    },
    "preview": {
      "screens": {
        "Patient": {
          "v13": {
            "objectCode": "Patient",
            "version": 13,
            "isPublished": true,
            "audit": {
              "tenantId": {
                "type": "text",
                "required": true
              },
              "createdAt": {
                "type": "datetime"
              },
              "createdBy": {
                "type": "text"
              },
              "updatedAt": {
                "type": "datetime"
              },
              "updatedBy": {
                "type": "text"
              },
              "isDeleted": {
                "type": "boolean",
                "default": false
              }
            },
            "fields": {
              "Name": {
                "ui": {
                  "type": "text"
                },
                "required": true,
                "unique": true
              },
              "Age": {
                "ui": {
                  "type": "number"
                },
                "required": true
              },
              "Email": {
                "ui": {
                  "type": "text"
                },
                "required": true
              }
            }
          }
        }
      }
    },
    "prod": {
      "screens": {
        "Doctor": {
          "v16": {
            "objectCode": "Doctor",
            "version": 16,
            "isPublished": true,
            "audit": {
              "tenantId": {
                "type": "text",
                "required": true
              },
              "createdAt": {
                "type": "datetime"
              },
              "createdBy": {
                "type": "text"
              },
              "updatedAt": {
                "type": "datetime"
              },
              "updatedBy": {
                "type": "text"
              },
              "isDeleted": {
                "type": "boolean",
                "default": false
              }
            },
            "uniqueConstraints": [
              [
                "firstName",
                "lastName"
              ]
            ],
            "fields": {
              "doctorCode": {
                "type": "String",
                "required": true,
                "unique": true,
                "ui": {
                  "label": "Doctor Code",
                  "type": "text"
                }
              },
              "firstName": {
                "type": "String",
                "ui": {
                  "type": "text"
                }
              },
              "lastName": {
                "type": "String",
                "ui": {
                  "type": "text"
                }
              },
              "specialization": {
                "type": "String",
                "ui": {
                  "type": "select",
                  "options": [
                    "General",
                    "Cardiology",
                    "Neurology",
                    "Pediatrics"
                  ]
                }
              },
              "licenseNumber": {
                "type": "String",
                "ui": {
                  "type": "text"
                }
              }
            }
          }
        },
        "PathologyBilling": {
          "v8": {
            "objectCode": "PathologyBilling",
            "version": 8,
            "isPublished": true,
            "audit": {
              "tenantId": {
                "type": "text",
                "required": true
              },
              "createdAt": {
                "type": "datetime"
              },
              "createdBy": {
                "type": "text"
              },
              "updatedAt": {
                "type": "datetime"
              },
              "updatedBy": {
                "type": "text"
              },
              "isDeleted": {
                "type": "boolean",
                "default": false
              }
            },
            "fields": {
              "BillNo": {
                "required": true,
                "unique": true,
                "autoGenerate": true,
                "pattern": "BILL-{YYYY}-{SEQ:6}",
                "ui": {
                  "type": "text",
                  "readOnly": true
                }
              },
              "BillingDate": {
                "ui": {
                  "type": "date"
                },
                "required": true
              },
              "PatientName": {
                "ui": {
                  "type": "text"
                },
                "required": true
              },
              "PatientAge": {
                "ui": {
                  "type": "number"
                }
              },
              "PatientGender": {
                "ui": {
                  "type": "select",
                  "options": [
                    "Male",
                    "Female",
                    "Other"
                  ]
                }
              },
              "ContactNumber": {
                "ui": {
                  "type": "tel"
                }
              },
              "Email": {
                "ui": {
                  "type": "email"
                }
              },
              "TotalAmount": {
                "ui": {
                  "type": "number"
                }
              },
              "NetAmount": {
                "ui": {
                  "type": "number"
                }
              }
            }
          }
        }
      }
    }
  },
  "meta": {
    "createdAt": "2026-01-15T10:10:00Z",
    "createdBy": "PlatformAdmin",
    "updatedAt": "2026-01-16T08:45:00Z",
    "updatedBy": "TenantAdmin"
  }
}
""";

            var document = BsonDocument.Parse(json);
            var filter = Builders<BsonDocument>.Filter.Eq("_id", document.GetValue("_id"));
            await collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true });

            Console.WriteLine("PlatformObjectTemplate seeded for tenant LAB_001.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: " + ex.Message);
        }
    }
}
