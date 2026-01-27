
export type Language = string;

export const translations: Record<string, any> = {
  en: {
    common: {
      save: "Save",
      cancel: "Cancel",
      edit: "Edit",
      delete: "Delete",
      view: "View",
      create: "Create",
      update: "Update",
      backToList: "Back to List",
      loading: "Loading...",
      noRecords: "No records found.",
      filters: "Filters",
      active: "Active",
      actions: "Actions",
      export: "Export Data",
      exportCSV: "Export as CSV",
      exportPDF: "Export as PDF",
      welcome: "Welcome to Valora",
      manage: "Manage your {{module}} records",
      new: "New {{module}}",
      success: "Success",
      error: "Error",
      operationFailed: "Operation Failed",
      validationError: "Validation Error",
      uniqueViolation: "This record cannot be saved because it duplicates an existing entry.",
      uniqueViolationHint: "Please modify the unique fields to ensure data integrity.",
      id: "ID",
      createdAt: "Created At",
      updatedAt: "Updated At",
      registration: "Registration",
      version: "Version",
      select: "Select",
      unique: "Unique",
      favorite: "Favourites",
      pin: "Pin",
      unpin: "Unpin",
      enter: "Enter {{field}}",
      required: "{{field}} is required",
      maxLength: "Max length is {{length}}",
      searchId: "Search ID...",
      filterBy: "Filter by...",
      all: "All",
      page: "Page",
      previous: "Previous",
      next: "Next",
      fields: {
        name: "Name",
        firstName: "First Name",
        lastName: "Last Name",
        email: "Email",
        phone: "Phone",
        dateOfBirth: "Date of Birth",
        gender: "Gender",
        address: "Address",
        city: "City",
        state: "State",
        zip: "Zip Code",
        country: "Country",
        status: "Status",
        description: "Description",
        notes: "Notes",
        code: "Code",
        type: "Type"
      }
    },
    messages: {
      common: {
        yes: "Yes",
        no: "No",
        ok: "OK",
        cancel: "Cancel",
        close: "Close",
        retry: "Retry",
        loading: "Loading, please wait...",
        processing: "Processing...",
        noData: "No data available",
        notAllowed: "You are not allowed to perform this action"
      },
      validation: {
        required: "This field is required",
        invalid: "Invalid value",
        minLength: "Minimum length is {{min}}",
        maxLength: "Maximum length is {{max}}",
        minValue: "Value must be at least {{min}}",
        maxValue: "Value must not exceed {{max}}",
        email: "Invalid email address",
        number: "Only numeric value allowed",
        date: "Invalid date format",
        unique: "This value already exists"
      },
      auth: {
        loginFailed: "Invalid username or password",
        sessionExpired: "Your session has expired. Please login again",
        unauthorized: "Unauthorized access",
        forbidden: "You do not have permission to access this screen"
      },
      network: {
        timeout: "Request timed out. Please try again",
        offline: "You are offline. Check your internet connection",
        serverError: "Server error occurred. Please contact support",
        unknownError: "Something went wrong. Please try again"
      },
      crud: {
        createSuccess: "{{entity}} created successfully",
        updateSuccess: "{{entity}} updated successfully",
        deleteSuccess: "{{entity}} deleted successfully",
        saveFailed: "Failed to save {{entity}}",
        deleteFailed: "Failed to delete {{entity}}",
        fetchFailed: "Failed to load {{entity}}"
      },
      confirmations: {
        delete: "Are you sure you want to delete this record?",
        discardChanges: "Discard unsaved changes?",
        logout: "Are you sure you want to logout?",
        publish: "Are you sure you want to publish this screen?",
        submit: "Do you want to submit this form?"
      },
      warnings: {
        unsavedChanges: "You have unsaved changes",
        readOnly: "This screen is read-only",
        partialSave: "Some data could not be saved",
        deprecatedField: "This field is deprecated"
      },
      success: {
        saved: "Saved successfully",
        published: "Published successfully",
        uploaded: "Uploaded successfully",
        processed: "Processed successfully"
      },
      screens: {
        generic: {
          create: "Create",
          edit: "Edit",
          view: "View",
          delete: "Delete",
          search: "Search",
          reset: "Reset",
          submit: "Submit",
          back: "Back"
        }
      },
      workflow: {
        draftSaved: "Draft saved successfully",
        publishSuccess: "Screen published successfully",
        publishFailed: "Screen publish failed",
        versionConflict: "Version conflict detected",
        locked: "This screen is currently locked for editing"
      },
      file: {
        uploadFailed: "File upload failed",
        fileTooLarge: "File size exceeds allowed limit",
        invalidType: "Invalid file type",
        downloadFailed: "File download failed"
      }
    },
    studio: {
      draft: {
        saved: "Draft saved!",
        errorPrefix: "Invalid JSON or Save Failed: "
      },
      unpublish: {
        blockedTitle: "Action Not Allowed",
        blockedMessage: "Modules under PROD environment cannot be unpublished.",
        confirmTitle: "Unpublish Module",
        confirmMessage: "Are you sure you want to unpublish this module? It will be hidden from users.",
        confirmAction: "Unpublish",
        success: "Unpublished successfully"
      },
      publish: {
        envInfoMissing: "Tenant or environment information not available for publish.",
        success: "Published successfully!",
        genericError: "Failed to publish.",
        noModuleSelected: "No module selected to publish.",
        selectTargetEnv: "Select a target environment for publish.",
        confirmTitle: "Publish Module",
        confirmMessage: "Are you sure you want to publish \"{{module}}\" from {{fromEnv}} to {{toEnv}}?",
        confirmAction: "Publish",
        noTargetEnv: "No target environment available",
        selectTargetEnvPlaceholder: "Select target environment"
      },
      ai: {
        settingsTitle: "Platform AI Settings",
        tooltip: "AI Settings",
        saveSuccess: "Configuration saved!",
        saveError: "Failed to save configuration",
        activateSuccess: "Configuration activated!",
        deleteTitle: "Delete Configuration",
        deleteMessage: "Are you sure you want to delete this configuration?",
        deleteConfirm: "Delete"
      },
      newModule: {
        title: "Create New Module",
        nameLabel: "Module Name",
        descriptionLabel: "Description / Requirements (for Trae AI)",
        namePlaceholder: "e.g. Appointment, Prescription, Inventory...",
        descriptionPlaceholder: "Describe the fields and purpose of this module...",
        createBlank: "Create Blank",
        generating: "Generating...",
        suggestWithAi: "Suggest with Trae AI"
      },
      bulkClone: {
        title: "Bulk Clone / Environment Sync",
        sourceEnv: "Source Environment",
        targetEnv: "Target Environment",
        selectModules: "Select Modules to Clone",
        selectAll: "Select All",
        clear: "Clear",
        selectedCount: "Selected: {{count}}",
        startAction: "Start Clone / Sync",
        statusStarting: "Starting bulk clone...",
        statusCloning: "Cloning {{module}}...",
        statusCompleted: "Completed: {{success}} successful, {{fail}} failed.",
        success: "Bulk Clone Completed ({{count}} synced)",
        envWarning: "Warning: You are currently connected to {{env}}. This action will save to {{env}}.",
        sidebarButton: "Bulk Clone / Sync"
      },
      clone: {
        title: "Clone Module",
        confirm: "Clone Module",
        newNameLabel: "New Module Name",
        namePlaceholder: "e.g. Patient_V2",
        success: "Module cloned as {{name}} (Source: {{source}}, Target: {{target}})"
      },
      moduleDelete: {
        title: "Delete Module",
        message: "Are you sure you want to permanently delete this module? This cannot be undone.",
        confirm: "Delete Forever",
        success: "Module deleted"
      }
    },
    patient: {
      fields: {
        firstName: "First Name",
        lastName: "Last Name",
        dateOfBirth: "Date of Birth",
        gender: "Gender",
        email: "Email",
        phone: "Phone",
        notes: "Medical Notes",
        patientCode: "Patient Code"
      }
    },
    doctor: {
      fields: {
        name: "Doctor Name",
        specialization: "Specialization",
        licenseNumber: "License Number",
        phone: "Phone",
        email: "Email",
        schedule: "Schedule"
      }
    },
    nav: {
      main: "Main",
      platform: "Platform",
      doctors: "Doctors",
      patients: "Patients",
      studio: "Studio",
      systemLogs: "System Logs",
      aiDashboard: "AI Dashboard",
      permissions: "Permissions",
      appearance: "Appearance",
      translations: "Translations",
      language: "Language",
      currency: "Currency"
    },
    systemLogs: {
      title: "Journaux d'erreurs système",
      loading: "Chargement des journaux...",
      errorTitle: "Erreur",
      confirmClearTitle: "Effacer tous les journaux",
      confirmClearMessage: "Voulez-vous vraiment effacer tous les journaux système ? Cette action est irréversible.",
      confirmClearConfirm: "Tout effacer",
      status: {
        fixed: "Corrigé",
        open: "Ouvert"
      },
      detail: {
        title: "Détails du journal",
        type: "Type",
        createdAt: "Créé le",
        status: "Statut",
        fixedBy: "Corrigé par",
        errorData: "Données d'erreur",
        fixSteps: "Étapes de correction",
        step: "Étape",
        stepType: "Type",
        stepStatus: "Statut",
        stepTime: "Heure",
        noFixSteps: "Aucune étape de correction automatisée enregistrée."
      },
      table: {
        type: "Type",
        message: "Message",
        createdAt: "Heure",
        status: "Statut",
        unknownError: "Erreur inconnue"
      },
      actions: {
        close: "Fermer",
        markAllFixed: "Tout marquer comme corrigé",
        markFixed: "Marquer comme corrigé",
        reopen: "Rouvrir"
      },
      errors: {
        fetchFailed: "Échec du chargement des journaux",
        updateStatusFailed: "Échec de la mise à jour du statut du journal",
        clearFailed: "Échec de la suppression des journaux"
      }
    },
    login: {
      title: "Bon retour",
      subtitle: "Connectez-vous pour accéder à votre tableau de bord",
      userProfileLabel: "Profil utilisateur",
      targetTenantLabel: "Locataire cible",
      enterSystem: "Entrer dans le système",
      enterSystemButton: "Entrer dans le système",
      environmentLabel: "Environnement",
      simulatedMode: "Connexion simulée (Mode Dev) - Aucun mot de passe requis",
      footer: "Connexion simulée (Mode Dev) - Aucun mot de passe requis"
    },
    permissions: {
      title: "Permissions",
      subtitle: "Gérer les permissions de la plateforme et les fonctionnalités",
      selectTenantLabel: "Sélectionner un locataire",
      tabs: {
        roles: "Rôles et permissions",
        features: "Fonctionnalités du locataire"
      },
      roles: {
        header: "Rôles",
        permissionsFor: "Permissions pour {{role}}",
        inputPlaceholder: "Saisir le code de permission (ex. CAN_VIEW_LOGS)",
        grantButton: "Accorder la permission",
        none: "Aucune permission attribuée à ce rôle."
      },
      features: {
        header: "Fonctionnalités du locataire",
        inputPlaceholder: "Saisir le code de fonctionnalité (ex. FEATURE_BULK_CLONE)",
        grantButton: "Activer la fonctionnalité",
        none: "Aucune fonctionnalité activée pour ce locataire.",
        enabled: "Activée"
      },
      toasts: {
        grantPermissionSuccess: "Permission {{code}} accordée",
        revokePermissionSuccess: "Permission {{code}} révoquée",
        grantFeatureSuccess: "Fonctionnalité {{code}} activée",
        revokeFeatureSuccess: "Fonctionnalité {{code}} désactivée"
      },
      errors: {
        grantPermissionFailed: "Échec de l'attribution de la permission",
        grantPermissionError: "Erreur lors de l'attribution de la permission",
        revokePermissionFailed: "Échec de la révocation de la permission",
        revokePermissionError: "Erreur lors de la révocation de la permission",
        grantFeatureFailed: "Échec de l'activation de la fonctionnalité",
        grantFeatureError: "Erreur lors de l'activation de la fonctionnalité",
        revokeFeatureFailed: "Échec de la désactivation de la fonctionnalité",
        revokeFeatureError: "Erreur lors de la désactivation de la fonctionnalité"
      },
      confirmations: {
        revokePermission: "Voulez-vous vraiment révoquer la permission {{code}} ?",
        revokeFeature: "Voulez-vous vraiment désactiver cette fonctionnalité ?"
      }
    },
    translationsManager: {
      title: "Gestionnaire de traductions",
      sourceLabel: "Source",
      targetLabel: "Cible",
      useEditorAsSource: "Utiliser le contenu de l’éditeur comme source",
      translateSchemaButton: "Traduire le schéma",
      translateSchemaTooltip: "Traduire la structure et les clés",
      autoTranslateButton: "Traduction automatique",
      autoTranslateTooltip: "Traduire les valeurs manquantes",
      refreshTooltip: "Actualiser",
      saveChangesButton: "Enregistrer les modifications",
      saving: "Enregistrement...",
      translating: "Traduction...",
      sidebarLanguages: "Langues",
      editorLabel: "JSON de traduction ({{lang}})",
      editorHint: "Modifiez directement le JSON. Les clés doivent correspondre à la structure.",
      messages: {
        saveSuccess: "Traductions enregistrées avec succès",
        autoTranslateSuccess: "Traduction automatique terminée",
        schemaTranslateSuccess: "Traduction du schéma terminée"
      },
      errors: {
        loadFailed: "Échec du chargement des traductions",
        invalidJsonFormat: "Format JSON invalide",
        saveFailed: "Échec de l’enregistrement des traductions",
        sourceTargetSame: "La source et la cible ne peuvent pas être identiques",
        invalidEditorJsonSource: "JSON invalide dans l’éditeur pour la source",
        autoTranslateFailed: "Échec de la traduction automatique",
        schemaTranslateEnableSource: "Vous devez activer « Utiliser le contenu de l’éditeur comme source » pour traduire le schéma",
        schemaTranslateInvalidJson: "JSON invalide dans l’éditeur",
        schemaTranslateFailed: "Échec de la traduction du schéma"
      }
    },
    errorBoundary: {
      title: "Un problème est survenu",
      message: "Une erreur inattendue s’est produite dans l’application.",
      reload: "Recharger l’application"
    },
    app: {
      theme: {
        light: "Clair",
        dark: "Sombre",
        midnight: "Minuit",
        sepia: "Sépia"
      },
      accentColor: "Couleur d’accent"
    }
  },
  es: {
    common: {
      save: "Guardar",
      cancel: "Cancelar",
      edit: "Editar",
      delete: "Eliminar",
      view: "Ver",
      create: "Crear",
      update: "Actualizar",
      backToList: "Volver a la lista",
      loading: "Cargando...",
      noRecords: "No se encontraron registros.",
      filters: "Filtros",
      active: "Activo",
      actions: "Acciones",
      export: "Exportar Datos",
      exportCSV: "Exportar como CSV",
      exportPDF: "Exportar como PDF",
      welcome: "Bienvenido a Valora",
      manage: "Administre sus registros de {{module}}",
      new: "Nuevo {{module}}",
      success: "Éxito",
      error: "Error",
      operationFailed: "Operación Fallida",
      validationError: "Error de Validación",
      uniqueViolation: "Este registro no se puede guardar porque duplica una entrada existente.",
      uniqueViolationHint: "Modifique los campos únicos para garantizar la integridad de los datos.",
      id: "ID",
      createdAt: "Creado En",
      updatedAt: "Actualizado En",
      registration: "Registro",
      version: "Versión",
      select: "Seleccionar",
      unique: "Único",
      favorite: "Favoritos",
      pin: "Fijar",
      unpin: "Quitar fijación",
      enter: "Ingrese {{field}}",
      required: "{{field}} es obligatorio",
      maxLength: "La longitud máxima es {{length}}",
      searchId: "Buscar ID...",
      filterBy: "Filtrar por...",
      all: "Todos",
      page: "Página",
      previous: "Anterior",
      next: "Siguiente",
      fields: {
        name: "Nombre",
        firstName: "Nombre",
        lastName: "Apellido",
        email: "Correo electrónico",
        phone: "Teléfono",
        dateOfBirth: "Fecha de nacimiento",
        gender: "Género",
        address: "Dirección",
        city: "Ciudad",
        state: "Estado",
        zip: "Código Postal",
        country: "País",
        status: "Estado",
        description: "Descripción",
        notes: "Notas",
        code: "Código",
        type: "Tipo"
      }
    },
    studio: {
      draft: {
        saved: "¡Borrador guardado!",
        errorPrefix: "JSON no válido o error al guardar: "
      },
      unpublish: {
        blockedTitle: "Acción no permitida",
        blockedMessage: "Los módulos en el entorno PROD no se pueden despublicar.",
        confirmTitle: "Despublicar módulo",
        confirmMessage: "¿Seguro que deseas despublicar este módulo? Se ocultará para los usuarios.",
        confirmAction: "Despublicar",
        success: "Despublicado correctamente"
      },
      publish: {
        envInfoMissing: "No hay información de inquilino o entorno para publicar.",
        success: "¡Publicado correctamente!",
        genericError: "Error al publicar.",
        noModuleSelected: "No hay módulo seleccionado para publicar.",
        selectTargetEnv: "Selecciona un entorno de destino para publicar.",
        confirmTitle: "Publicar módulo",
        confirmMessage: "¿Seguro que deseas publicar \"{{module}}\" de {{fromEnv}} a {{toEnv}}?",
        confirmAction: "Publicar",
        noTargetEnv: "No hay entorno de destino disponible",
        selectTargetEnvPlaceholder: "Selecciona entorno de destino"
      },
      ai: {
        settingsTitle: "Configuración de IA de la plataforma",
        tooltip: "Configuración de IA",
        saveSuccess: "¡Configuración guardada!",
        saveError: "Error al guardar la configuración",
        activateSuccess: "¡Configuración activada!",
        deleteTitle: "Eliminar configuración",
        deleteMessage: "¿Seguro que deseas eliminar esta configuración?",
        deleteConfirm: "Eliminar"
      },
      newModule: {
        title: "Crear nuevo módulo",
        nameLabel: "Nombre del módulo",
        descriptionLabel: "Descripción / Requisitos (para Trae IA)",
        namePlaceholder: "p.ej. Citas, Recetas, Inventario...",
        descriptionPlaceholder: "Describe los campos y el propósito de este módulo...",
        createBlank: "Crear en blanco",
        generating: "Generando...",
        suggestWithAi: "Sugerir con Trae IA"
      },
      bulkClone: {
        title: "Clonado masivo / Sincronización de entornos",
        sourceEnv: "Entorno origen",
        targetEnv: "Entorno destino",
        selectModules: "Seleccionar módulos a clonar",
        selectAll: "Seleccionar todo",
        clear: "Limpiar selección",
        selectedCount: "Seleccionados: {{count}}",
        startAction: "Iniciar clonado / sincronización",
        statusStarting: "Iniciando clonado masivo...",
        statusCloning: "Clonando {{module}}...",
        statusCompleted: "Completado: {{success}} correctos, {{fail}} con error.",
        success: "Clonado masivo completado ({{count}} sincronizados)",
        envWarning: "Advertencia: Actualmente estás conectado a {{env}}. Esta acción guardará en {{env}}.",
        sidebarButton: "Clonado masivo / Sincronización"
      },
      clone: {
        title: "Clonar módulo",
        confirm: "Clonar módulo",
        newNameLabel: "Nuevo nombre de módulo",
        namePlaceholder: "p.ej. Paciente_V2",
        success: "Módulo clonado como {{name}} (Origen: {{source}}, Destino: {{target}})"
      },
      moduleDelete: {
        title: "Eliminar módulo",
        message: "¿Seguro que deseas eliminar permanentemente este módulo? Esta acción no se puede deshacer.",
        confirm: "Eliminar para siempre",
        success: "Módulo eliminado"
      }
    },
    patient: {
      fields: {
        firstName: "Nombre",
        lastName: "Apellido",
        dateOfBirth: "Fecha de nacimiento",
        gender: "Género",
        email: "Correo electrónico",
        phone: "Teléfono",
        notes: "Notas Médicas",
        patientCode: "Código del Paciente"
      }
    },
    doctor: {
      fields: {
        name: "Nombre del Doctor",
        specialization: "Especialización",
        licenseNumber: "Número de Licencia",
        phone: "Teléfono",
        email: "Correo electrónico",
        schedule: "Horario"
      }
    },
    nav: {
      main: "Principal",
      platform: "Plataforma",
      doctors: "Doctores",
      patients: "Pacientes",
      studio: "Estudio",
      systemLogs: "Registros del Sistema",
      aiDashboard: "Panel de IA",
      permissions: "Permisos",
      appearance: "Apariencia",
      translations: "Traducciones",
      language: "Idioma",
      currency: "Moneda"
    },
    systemLogs: {
      title: "Registros de Errores del Sistema",
      loading: "Cargando registros...",
      errorTitle: "Error",
      confirmClearTitle: "Borrar Todos los Registros",
      confirmClearMessage: "¿Está seguro de que desea borrar todos los registros del sistema? Esto no se puede deshacer.",
      confirmClearConfirm: "Borrar Todo",
      status: {
        fixed: "Corregido",
        open: "Abierto"
      },
      detail: {
        title: "Detalles del Registro",
        type: "Tipo",
        createdAt: "Creado En",
        status: "Estado",
        fixedBy: "Corregido Por",
        errorData: "Datos del Error",
        fixSteps: "Pasos de Corrección",
        step: "Paso",
        stepType: "Tipo",
        stepStatus: "Estado",
        stepTime: "Hora",
        noFixSteps: "No se registraron pasos de corrección automatizados."
      },
      table: {
        type: "Tipo",
        message: "Mensaje",
        createdAt: "Hora",
        status: "Estado",
        unknownError: "Error Desconocido"
      },
      actions: {
        close: "Cerrar",
        markAllFixed: "Marcar Todo como Corregido",
        markFixed: "Marcar Corregido",
        reopen: "Reabrir"
      },
      errors: {
        fetchFailed: "Error al cargar registros",
        updateStatusFailed: "Error al actualizar estado del registro",
        clearFailed: "Error al borrar registros"
      }
    },
    login: {
      title: "Bienvenido de nuevo",
      subtitle: "Inicie sesión para acceder a su panel",
      userProfileLabel: "Perfil de Usuario",
      targetTenantLabel: "Inquilino Objetivo",
      enterSystem: "Entrar al Sistema",
      enterSystemButton: "Entrar al Sistema",
      environmentLabel: "Entorno",
      simulatedMode: "Inicio de Sesión Simulado (Modo Dev) - Sin contraseña requerida",
      footer: "Inicio de Sesión Simulado (Modo Dev) - Sin contraseña requerida"
    },
    permissions: {
      title: "Permisos de Plataforma",
      subtitle: "Gestionar permisos de usuario y banderas de características",
      selectTenantLabel: "Seleccionar inquilino",
      tabs: {
        roles: "Roles y Permisos",
        features: "Características del inquilino"
      },
      roles: {
        header: "Roles",
        permissionsFor: "Permisos para {{role}}",
        inputPlaceholder: "Ingrese el código de permiso (ej. CAN_VIEW_LOGS)",
        grantButton: "Conceder permiso",
        none: "Sin permisos asignados a este rol."
      },
      features: {
        header: "Características del inquilino",
        inputPlaceholder: "Ingrese el código de característica (ej. FEATURE_BULK_CLONE)",
        grantButton: "Habilitar característica",
        none: "Sin características habilitadas para este inquilino.",
        enabled: "Habilitado"
      },
      toasts: {
        grantPermissionSuccess: "Permiso {{code}} concedido",
        revokePermissionSuccess: "Permiso {{code}} revocado",
        grantFeatureSuccess: "Característica {{code}} habilitada",
        revokeFeatureSuccess: "Característica {{code}} deshabilitada"
      },
      errors: {
        grantPermissionFailed: "Error al conceder permiso",
        grantPermissionError: "Error al conceder permiso",
        revokePermissionFailed: "Error al revocar permiso",
        revokePermissionError: "Error al revocar permiso",
        grantFeatureFailed: "Error al habilitar característica",
        grantFeatureError: "Error al habilitar característica",
        revokeFeatureFailed: "Error al deshabilitar característica",
        revokeFeatureError: "Error al deshabilitar característica"
      },
      confirmations: {
        revokePermission: "¿Está seguro de que desea revocar el permiso {{code}}?",
        revokeFeature: "¿Está seguro de que desea deshabilitar esta característica?"
      }
    },
    translationsManager: {
      title: "Gestor de Traducciones",
      sourceLabel: "Fuente",
      targetLabel: "Destino",
      useEditorAsSource: "Usar contenido del editor como fuente",
      translateSchemaButton: "Traducir Esquema",
      translateSchemaTooltip: "Traducir estructura y claves",
      autoTranslateButton: "Auto Traducir",
      autoTranslateTooltip: "Traducir valores faltantes",
      refreshTooltip: "Actualizar",
      saveChangesButton: "Guardar Cambios",
      saving: "Guardando...",
      translating: "Traduciendo...",
      sidebarLanguages: "Idiomas",
      editorLabel: "JSON de Traducción ({{lang}})",
      editorHint: "Edite el JSON directamente. Las claves deben coincidir con la estructura.",
      messages: {
        saveSuccess: "Traducciones guardadas exitosamente",
        autoTranslateSuccess: "Auto-traducción completada",
        schemaTranslateSuccess: "Traducción de esquema completada"
      },
      errors: {
        loadFailed: "Error al cargar traducciones",
        invalidJsonFormat: "Formato JSON inválido",
        saveFailed: "Error al guardar traducciones",
        sourceTargetSame: "Los idiomas de origen y destino no pueden ser los mismos",
        invalidEditorJsonSource: "JSON inválido en el editor para la fuente",
        autoTranslateFailed: "Auto-traducción fallida",
        schemaTranslateEnableSource: "Debe habilitar 'Usar contenido del editor como fuente' para la traducción de esquema",
        schemaTranslateInvalidJson: "JSON inválido en el editor",
        schemaTranslateFailed: "Traducción de esquema fallida"
      }
    },
    errorBoundary: {
      title: "Algo salió mal",
      message: "Ha ocurrido un error inesperado en la aplicación.",
      reload: "Recargar Aplicación"
    },
    app: {
      theme: {
        light: "Claro",
        dark: "Oscuro",
        midnight: "Medianoche",
        sepia: "Sepia"
      },
      accentColor: "Color de Acento"
    }
  },
  fr: {
    common: {
      save: "Enregistrer",
      cancel: "Annuler",
      edit: "Modifier",
      delete: "Supprimer",
      view: "Voir",
      create: "Créer",
      update: "Mettre à jour",
      backToList: "Retour à la liste",
      loading: "Chargement...",
      noRecords: "Aucun enregistrement trouvé.",
      filters: "Filtres",
      active: "Actif",
      actions: "Actions",
      export: "Exporter les données",
      exportCSV: "Exporter en CSV",
      exportPDF: "Exporter en PDF",
      welcome: "Bienvenue sur Valora",
      manage: "Gérez vos enregistrements {{module}}",
      new: "Nouveau {{module}}",
      success: "Succès",
      error: "Erreur",
      operationFailed: "Opération échouée",
      validationError: "Erreur de validation",
      uniqueViolation: "Cet enregistrement ne peut pas être sauvegardé car il duplique une entrée existante.",
      uniqueViolationHint: "Veuillez modifier les champs uniques pour garantir l'intégrité des données.",
      id: "ID",
      createdAt: "Créé le",
      updatedAt: "Mis à jour le",
      registration: "Inscription",
      version: "Version",
      select: "Sélectionner",
      unique: "Unique",
      favorite: "Favoris",
      pin: "Épingler",
      unpin: "Désépingler",
      enter: "Entrez {{field}}",
      required: "{{field}} est requis",
      maxLength: "Longueur maximale {{length}}",
      searchId: "Rechercher ID...",
      filterBy: "Filtrer par...",
      all: "Tous",
      page: "Page",
      previous: "Précédent",
      next: "Suivant",
      fields: {
        name: "Nom",
        firstName: "Prénom",
        lastName: "Nom",
        email: "Email",
        phone: "Téléphone",
        dateOfBirth: "Date de naissance",
        gender: "Genre",
        address: "Adresse",
        city: "Ville",
        state: "État",
        zip: "Code Postal",
        country: "Pays",
        status: "Statut",
        description: "Description",
        notes: "Notes",
        code: "Code",
        type: "Type"
      }
    },
    studio: {
      draft: {
        saved: "Brouillon enregistré !",
        errorPrefix: "JSON invalide ou erreur d’enregistrement : "
      },
      unpublish: {
        blockedTitle: "Action non autorisée",
        blockedMessage: "Les modules en environnement PROD ne peuvent pas être dépubliés.",
        confirmTitle: "Dépublier le module",
        confirmMessage: "Êtes-vous sûr de vouloir dépublier ce module ? Il sera masqué pour les utilisateurs.",
        confirmAction: "Dépublier",
        success: "Dépublication réussie"
      },
      publish: {
        envInfoMissing: "Informations de locataire ou d’environnement manquantes pour publier.",
        success: "Publication réussie !",
        genericError: "Échec de la publication.",
        noModuleSelected: "Aucun module sélectionné pour la publication.",
        selectTargetEnv: "Sélectionnez un environnement cible pour la publication.",
        confirmTitle: "Publier le module",
        confirmMessage: "Voulez-vous vraiment publier \"{{module}}\" de {{fromEnv}} vers {{toEnv}} ?",
        confirmAction: "Publier",
        noTargetEnv: "Aucun environnement cible disponible",
        selectTargetEnvPlaceholder: "Sélectionner l’environnement cible"
      },
      ai: {
        settingsTitle: "Paramètres IA de la plateforme",
        tooltip: "Paramètres IA",
        saveSuccess: "Configuration enregistrée !",
        saveError: "Échec de l’enregistrement de la configuration",
        activateSuccess: "Configuration activée !",
        deleteTitle: "Supprimer la configuration",
        deleteMessage: "Voulez-vous vraiment supprimer cette configuration ?",
        deleteConfirm: "Supprimer"
      },
      newModule: {
        title: "Créer un nouveau module",
        nameLabel: "Nom du module",
        descriptionLabel: "Description / Exigences (pour Trae IA)",
        namePlaceholder: "ex. Rendez-vous, Ordonnances, Inventaire...",
        descriptionPlaceholder: "Décrivez les champs et l’objectif de ce module...",
        createBlank: "Créer vierge",
        generating: "Génération...",
        suggestWithAi: "Suggérer avec Trae IA"
      },
      bulkClone: {
        title: "Clonage massif / Synchronisation d’environnements",
        sourceEnv: "Environnement source",
        targetEnv: "Environnement cible",
        selectModules: "Sélectionner les modules à cloner",
        selectAll: "Tout sélectionner",
        clear: "Effacer la sélection",
        selectedCount: "Sélectionnés : {{count}}",
        startAction: "Démarrer le clonage / la synchro",
        statusStarting: "Démarrage du clonage massif...",
        statusCloning: "Clonage de {{module}}...",
        statusCompleted: "Terminé : {{success}} réussis, {{fail}} en échec.",
        success: "Clonage massif terminé ({{count}} synchronisés)",
        envWarning: "Attention : vous êtes actuellement connecté à {{env}}. Cette action enregistrera sur {{env}}.",
        sidebarButton: "Clonage massif / Synchro"
      },
      clone: {
        title: "Cloner le module",
        confirm: "Cloner le module",
        newNameLabel: "Nouveau nom du module",
        namePlaceholder: "ex. Patient_V2",
        success: "Module cloné en {{name}} (Source : {{source}}, Cible : {{target}})"
      },
      moduleDelete: {
        title: "Supprimer le module",
        message: "Voulez-vous vraiment supprimer définitivement ce module ? Cette action est irréversible.",
        confirm: "Supprimer définitivement",
        success: "Module supprimé"
      }
    },
    patient: {
      fields: {
        firstName: "Prénom",
        lastName: "Nom",
        dateOfBirth: "Date de naissance",
        gender: "Genre",
        email: "Email",
        phone: "Téléphone",
        notes: "Notes Médicales",
        patientCode: "Code Patient"
      }
    },
    doctor: {
      fields: {
        name: "Nom du Docteur",
        specialization: "Spécialisation",
        licenseNumber: "Numéro de Licence",
        phone: "Téléphone",
        email: "Email",
        schedule: "Emploi du temps"
      }
    },
    nav: {
      main: "Principal",
      platform: "Plateforme",
      doctors: "Médecins",
      patients: "Patients",
      studio: "Studio",
      systemLogs: "Journaux système",
      aiDashboard: "Tableau de bord IA",
      permissions: "Permissions",
      appearance: "Apparence",
      translations: "Traductions",
      language: "Langue",
      currency: "Devise"
    },
    systemLogs: {
      title: "Systemfehlerprotokolle",
      loading: "Protokolle werden geladen...",
      errorTitle: "Fehler",
      confirmClearTitle: "Alle Protokolle löschen",
      confirmClearMessage: "Möchten Sie wirklich alle Systemprotokolle löschen? Dies kann nicht rückgängig gemacht werden.",
      confirmClearConfirm: "Alles löschen",
      status: {
        fixed: "Behoben",
        open: "Offen"
      },
      detail: {
        title: "Protokolldetails",
        type: "Typ",
        createdAt: "Erstellt am",
        status: "Status",
        fixedBy: "Behoben von",
        errorData: "Fehlerdaten",
        fixSteps: "Durchgeführte Schritte",
        step: "Schritt",
        stepType: "Typ",
        stepStatus: "Status",
        stepTime: "Zeit",
        noFixSteps: "Keine automatischen Korrekturschritte aufgezeichnet."
      },
      table: {
        type: "Typ",
        message: "Nachricht",
        createdAt: "Zeit",
        status: "Status",
        unknownError: "Unbekannter Fehler"
      },
      actions: {
        close: "Schließen",
        markAllFixed: "Alle als behoben markieren",
        markFixed: "Als behoben markieren",
        reopen: "Wieder öffnen"
      },
      errors: {
        fetchFailed: "Protokolle konnten nicht geladen werden",
        updateStatusFailed: "Protokollstatus konnte nicht aktualisiert werden",
        clearFailed: "Protokolle konnten nicht gelöscht werden"
      }
    },
    login: {
      title: "Willkommen zurück",
      subtitle: "Melden Sie sich an, um auf Ihr Dashboard zuzugreifen",
      userProfileLabel: "Benutzerprofil",
      targetTenantLabel: "Zielmandant",
      enterSystem: "System betreten",
      enterSystemButton: "System betreten",
      environmentLabel: "Umgebung",
      simulatedMode: "Simulierte Anmeldung (Dev-Modus) – Kein Passwort erforderlich",
      footer: "Simulierte Anmeldung (Dev-Modus) – Kein Passwort erforderlich"
    },
    permissions: {
      title: "Berechtigungen",
      subtitle: "Plattformberechtigungen und Funktionsflags verwalten",
      selectTenantLabel: "Mandanten auswählen",
      tabs: {
        roles: "Rollen und Berechtigungen",
        features: "Mandantenfunktionen"
      },
      roles: {
        header: "Rollen",
        permissionsFor: "Berechtigungen für {{role}}",
        inputPlaceholder: "Berechtigungscode eingeben (z. B. CAN_VIEW_LOGS)",
        grantButton: "Berechtigung gewähren",
        none: "Dieser Rolle sind noch keine Berechtigungen zugewiesen."
      },
      features: {
        header: "Mandantenfunktionen",
        inputPlaceholder: "Funktionscode eingeben (z. B. FEATURE_BULK_CLONE)",
        grantButton: "Funktion aktivieren",
        none: "Für diesen Mandanten sind noch keine Funktionen aktiviert.",
        enabled: "Aktiv"
      },
      toasts: {
        grantPermissionSuccess: "Berechtigung {{code}} gewährt",
        revokePermissionSuccess: "Berechtigung {{code}} entzogen",
        grantFeatureSuccess: "Funktion {{code}} aktiviert",
        revokeFeatureSuccess: "Funktion {{code}} deaktiviert"
      },
      errors: {
        grantPermissionFailed: "Berechtigung konnte nicht gewährt werden",
        grantPermissionError: "Fehler beim Gewähren der Berechtigung",
        revokePermissionFailed: "Berechtigung konnte nicht entzogen werden",
        revokePermissionError: "Fehler beim Entziehen der Berechtigung",
        grantFeatureFailed: "Funktion konnte nicht aktiviert werden",
        grantFeatureError: "Fehler beim Aktivieren der Funktion",
        revokeFeatureFailed: "Funktion konnte nicht deaktiviert werden",
        revokeFeatureError: "Fehler beim Deaktivieren der Funktion"
      },
      confirmations: {
        revokePermission: "Möchten Sie die Berechtigung {{code}} wirklich entziehen?",
        revokeFeature: "Möchten Sie diese Funktion wirklich deaktivieren?"
      }
    },
    translationsManager: {
      title: "Übersetzungsverwaltung",
      sourceLabel: "Quelle",
      targetLabel: "Ziel",
      useEditorAsSource: "Editorinhalt als Quelle verwenden",
      translateSchemaButton: "Schema übersetzen",
      translateSchemaTooltip: "Struktur und Schlüssel übersetzen",
      autoTranslateButton: "Automatisch übersetzen",
      autoTranslateTooltip: "Fehlende Werte übersetzen",
      refreshTooltip: "Aktualisieren",
      saveChangesButton: "Änderungen speichern",
      saving: "Speichern...",
      translating: "Übersetzen...",
      sidebarLanguages: "Sprachen",
      editorLabel: "Übersetzungs-JSON ({{lang}})",
      editorHint: "Bearbeiten Sie das JSON direkt. Schlüssel müssen der Struktur entsprechen.",
      messages: {
        saveSuccess: "Übersetzungen erfolgreich gespeichert",
        autoTranslateSuccess: "Automatische Übersetzung abgeschlossen",
        schemaTranslateSuccess: "Schemaübersetzung abgeschlossen"
      },
      errors: {
        loadFailed: "Übersetzungen konnten nicht geladen werden",
        invalidJsonFormat: "Ungültiges JSON-Format",
        saveFailed: "Übersetzungen konnten nicht gespeichert werden",
        sourceTargetSame: "Quelle und Ziel dürfen nicht identisch sein",
        invalidEditorJsonSource: "Ungültiges JSON im Editor für die Quelle",
        autoTranslateFailed: "Automatische Übersetzung fehlgeschlagen",
        schemaTranslateEnableSource: "Sie müssen „Editorinhalt als Quelle verwenden“ aktivieren, um das Schema zu übersetzen",
        schemaTranslateInvalidJson: "Ungültiges JSON im Editor",
        schemaTranslateFailed: "Schemaübersetzung fehlgeschlagen"
      }
    },
    errorBoundary: {
      title: "Es ist ein Fehler aufgetreten",
      message: "In der Anwendung ist ein unerwarteter Fehler aufgetreten.",
      reload: "Anwendung neu laden"
    },
    app: {
      theme: {
        light: "Hell",
        dark: "Dunkel",
        midnight: "Mitternacht",
        sepia: "Sepia"
      },
      accentColor: "Akzentfarbe"
    }
  },
  de: {
    common: {
      save: "Speichern",
      cancel: "Abbrechen",
      edit: "Bearbeiten",
      delete: "Löschen",
      view: "Ansehen",
      create: "Erstellen",
      update: "Aktualisieren",
      backToList: "Zurück zur Liste",
      loading: "Laden...",
      noRecords: "Keine Datensätze gefunden.",
      filters: "Filter",
      active: "Aktiv",
      actions: "Aktionen",
      export: "Daten exportieren",
      exportCSV: "Als CSV exportieren",
      exportPDF: "Als PDF exportieren",
      welcome: "Willkommen bei Valora",
      manage: "Verwalten Sie Ihre {{module}}-Datensätze",
      new: "Neu {{module}}",
      success: "Erfolg",
      error: "Fehler",
      operationFailed: "Vorgang fehlgeschlagen",
      validationError: "Validierungsfehler",
      uniqueViolation: "Dieser Datensatz kann nicht gespeichert werden, da er einen vorhandenen Eintrag dupliziert.",
      uniqueViolationHint: "Bitte ändern Sie die eindeutigen Felder, um die Datenintegrität zu gewährleisten.",
      id: "ID",
      createdAt: "Erstellt am",
      updatedAt: "Aktualisiert am",
      registration: "Registrierung",
      version: "Version",
      select: "Auswählen",
      unique: "Einzigartig",
      favorite: "Favoriten",
      pin: "Anheften",
      unpin: "Lösen",
      searchId: "ID suchen...",
      filterBy: "Filtern nach...",
      enter: "{{field}} eingeben",
      required: "{{field}} ist erforderlich",
      maxLength: "Maximale Länge ist {{length}}",
      all: "Alle",
      page: "Seite",
      previous: "Vorherige",
      next: "Nächste",
      fields: {
        name: "Name",
        firstName: "Vorname",
        lastName: "Nachname",
        email: "E-Mail",
        phone: "Telefon",
        dateOfBirth: "Geburtsdatum",
        gender: "Geschlecht",
        address: "Adresse",
        city: "Stadt",
        state: "Bundesland",
        zip: "Postleitzahl",
        country: "Land",
        status: "Status",
        description: "Beschreibung",
        notes: "Notizen",
        code: "Code",
        type: "Typ"
      }
    },
    studio: {
      draft: {
        saved: "Entwurf gespeichert!",
        errorPrefix: "Ungültiges JSON oder Speicherfehler: "
      },
      unpublish: {
        blockedTitle: "Aktion nicht erlaubt",
        blockedMessage: "Module in der PROD-Umgebung können nicht unpubliziert werden.",
        confirmTitle: "Modul unpublizieren",
        confirmMessage: "Möchten Sie dieses Modul wirklich unpublizieren? Es wird für Benutzer ausgeblendet.",
        confirmAction: "Unpublizieren",
        success: "Erfolgreich unpubliziert"
      },
      publish: {
        envInfoMissing: "Mieter- oder Umgebungsinformationen zum Veröffentlichen fehlen.",
        success: "Erfolgreich veröffentlicht!",
        genericError: "Veröffentlichen fehlgeschlagen.",
        noModuleSelected: "Kein Modul zum Veröffentlichen ausgewählt.",
        selectTargetEnv: "Wählen Sie eine Zielumgebung zum Veröffentlichen.",
        confirmTitle: "Modul veröffentlichen",
        confirmMessage: "Möchten Sie \"{{module}}\" wirklich von {{fromEnv}} nach {{toEnv}} veröffentlichen?",
        confirmAction: "Veröffentlichen",
        noTargetEnv: "Keine Zielumgebung verfügbar",
        selectTargetEnvPlaceholder: "Zielumgebung auswählen"
      },
      ai: {
        settingsTitle: "Plattform-KI-Einstellungen",
        tooltip: "KI-Einstellungen",
        saveSuccess: "Konfiguration gespeichert!",
        saveError: "Konfiguration konnte nicht gespeichert werden",
        activateSuccess: "Konfiguration aktiviert!",
        deleteTitle: "Konfiguration löschen",
        deleteMessage: "Möchten Sie diese Konfiguration wirklich löschen?",
        deleteConfirm: "Löschen"
      },
      newModule: {
        title: "Neues Modul erstellen",
        nameLabel: "Modulname",
        descriptionLabel: "Beschreibung / Anforderungen (für Trae KI)",
        namePlaceholder: "z. B. Termine, Rezepte, Inventar...",
        descriptionPlaceholder: "Beschreiben Sie die Felder und den Zweck dieses Moduls...",
        createBlank: "Leer erstellen",
        generating: "Wird generiert...",
        suggestWithAi: "Mit Trae KI vorschlagen"
      },
      bulkClone: {
        title: "Massenklon / Umgebungsabgleich",
        sourceEnv: "Quellumgebung",
        targetEnv: "Zielumgebung",
        selectModules: "Zu klonende Module auswählen",
        selectAll: "Alle auswählen",
        clear: "Auswahl aufheben",
        selectedCount: "Ausgewählt: {{count}}",
        startAction: "Klonen / Abgleichen starten",
        statusStarting: "Massenklon wird gestartet...",
        statusCloning: "{{module}} wird geklont...",
        statusCompleted: "Abgeschlossen: {{success}} erfolgreich, {{fail}} fehlgeschlagen.",
        success: "Massenklon abgeschlossen ({{count}} synchronisiert)",
        envWarning: "Warnung: Sie sind derzeit mit {{env}} verbunden. Diese Aktion speichert in {{env}}.",
        sidebarButton: "Massenklon / Abgleich"
      },
      clone: {
        title: "Modul klonen",
        confirm: "Modul klonen",
        newNameLabel: "Neuer Modulname",
        namePlaceholder: "z. B. Patient_V2",
        success: "Modul wurde als {{name}} geklont (Quelle: {{source}}, Ziel: {{target}})"
      },
      moduleDelete: {
        title: "Modul löschen",
        message: "Möchten Sie dieses Modul wirklich dauerhaft löschen? Dies kann nicht rückgängig gemacht werden.",
        confirm: "Für immer löschen",
        success: "Modul gelöscht"
      }
    },
    patient: {
      fields: {
        firstName: "Vorname",
        lastName: "Nachname",
        dateOfBirth: "Geburtsdatum",
        gender: "Geschlecht",
        email: "E-Mail",
        phone: "Telefon",
        notes: "Medizinische Notizen",
        patientCode: "Patientencode"
      }
    },
    doctor: {
      fields: {
        name: "Arztname",
        specialization: "Spezialisierung",
        licenseNumber: "Lizenznummer",
        phone: "Telefon",
        email: "E-Mail",
        schedule: "Zeitplan"
      }
    },
    nav: {
      main: "Hauptmenü",
      platform: "Plattform",
      doctors: "Ärzte",
      patients: "Patienten",
      studio: "Studio",
      systemLogs: "Systemprotokolle",
      aiDashboard: "KI-Dashboard",
      permissions: "Berechtigungen",
      appearance: "Aussehen",
      translations: "Übersetzungen",
      language: "Sprache",
      currency: "Währung"
    },
    systemLogs: {
      title: "System Error Logs",
      loading: "Loading logs...",
      errorTitle: "Error",
      confirmClearTitle: "Clear All Logs",
      confirmClearMessage: "Are you sure you want to clear all system logs? This cannot be undone.",
      confirmClearConfirm: "Clear All",
      status: {
        fixed: "Fixed",
        open: "Open"
      },
      detail: {
        title: "Log Details",
        type: "Type",
        createdAt: "Created At",
        status: "Status",
        fixedBy: "Fixed By",
        errorData: "Error Data",
        fixSteps: "Fix Steps Taken",
        step: "Step",
        stepType: "Type",
        stepStatus: "Status",
        stepTime: "Time",
        noFixSteps: "No automated fix steps recorded."
      },
      table: {
        type: "Type",
        message: "Message",
        createdAt: "Time",
        status: "Status",
        unknownError: "Unknown Error"
      },
      actions: {
        close: "Close",
        markAllFixed: "Mark All as Fixed",
        markFixed: "Mark Fixed",
        reopen: "Reopen"
      },
      errors: {
        fetchFailed: "Failed to load logs",
        updateStatusFailed: "Failed to update log status",
        clearFailed: "Failed to clear logs"
      }
    },
    login: {
      title: "Welcome Back",
      subtitle: "Sign in to access your dashboard",
      userProfileLabel: "User Profile",
      targetTenantLabel: "Target Tenant",
      enterSystem: "Enter System",
      enterSystemButton: "Enter System",
      environmentLabel: "Environment",
      simulatedMode: "Simulated Login (Dev Mode) - No password required",
      footer: "Simulated Login (Dev Mode) - No password required"
    },
    permissions: {
      title: "Permissions",
      subtitle: "Manage platform permissions and feature flags",
      selectTenantLabel: "Select Tenant",
      tabs: {
        roles: "Roles & Permissions",
        features: "Tenant Features"
      },
      roles: {
        header: "Roles",
        permissionsFor: "Permissions for {{role}}",
        inputPlaceholder: "Enter permission code (e.g. CAN_VIEW_LOGS)",
        grantButton: "Grant Permission",
        none: "No permissions assigned to this role yet."
      },
      features: {
        header: "Tenant Features",
        inputPlaceholder: "Enter feature code (e.g. FEATURE_BULK_CLONE)",
        grantButton: "Enable Feature",
        none: "No features enabled for this tenant yet.",
        enabled: "Enabled"
      },
      toasts: {
        grantPermissionSuccess: "Permission {{code}} granted",
        revokePermissionSuccess: "Permission {{code}} revoked",
        grantFeatureSuccess: "Feature {{code}} enabled",
        revokeFeatureSuccess: "Feature {{code}} disabled"
      },
      errors: {
        grantPermissionFailed: "Failed to grant permission",
        grantPermissionError: "Error granting permission",
        revokePermissionFailed: "Failed to revoke permission",
        revokePermissionError: "Error revoking permission",
        grantFeatureFailed: "Failed to enable feature",
        grantFeatureError: "Error enabling feature",
        revokeFeatureFailed: "Failed to disable feature",
        revokeFeatureError: "Error disabling feature"
      },
      confirmations: {
        revokePermission: "Are you sure you want to revoke permission {{code}}?",
        revokeFeature: "Are you sure you want to disable this feature?"
      }
    },
    translationsManager: {
      title: "Translation Manager",
      sourceLabel: "Source",
      targetLabel: "Target",
      useEditorAsSource: "Use Editor Content as Source",
      translateSchemaButton: "Translate Schema",
      translateSchemaTooltip: "Translate structure and keys",
      autoTranslateButton: "Auto Translate",
      autoTranslateTooltip: "Translate missing values",
      refreshTooltip: "Refresh",
      saveChangesButton: "Save Changes",
      saving: "Saving...",
      translating: "Translating...",
      sidebarLanguages: "Languages",
      editorLabel: "Translation JSON ({{lang}})",
      editorHint: "Edit the JSON directly. Keys must match the structure.",
      messages: {
        saveSuccess: "Translations saved successfully",
        autoTranslateSuccess: "Auto-translation completed",
        schemaTranslateSuccess: "Schema translation completed"
      },
      errors: {
        loadFailed: "Failed to load translations",
        invalidJsonFormat: "Invalid JSON format",
        saveFailed: "Failed to save translations",
        sourceTargetSame: "Source and target languages cannot be the same",
        invalidEditorJsonSource: "Invalid JSON in editor for source",
        autoTranslateFailed: "Auto-translation failed",
        schemaTranslateEnableSource: "You must enable 'Use Editor Content as Source' for schema translation",
        schemaTranslateInvalidJson: "Invalid JSON in editor",
        schemaTranslateFailed: "Schema translation failed"
      }
    },
    errorBoundary: {
      title: "Something went wrong",
      message: "An unexpected error has occurred in the application.",
      reload: "Reload Application"
    },
    app: {
      theme: {
        light: "Light",
        dark: "Dark",
        midnight: "Midnight",
        sepia: "Sepia"
      },
      accentColor: "Accent Color"
    }
  },
  hi: {
    common: {
      save: "सहेजें",
      cancel: "रद्द करें",
      edit: "संपादित करें",
      delete: "हटाएं",
      view: "देखें",
      create: "बनाएं",
      update: "अपडेट करें",
      backToList: "सूची पर वापस जाएं",
      loading: "लोड हो रहा है...",
      noRecords: "कोई रिकॉर्ड नहीं मिला।",
      filters: "फ़िल्टर",
      active: "सक्रिय",
      actions: "क्रियाएं",
      export: "डेटा निर्यात करें",
      exportCSV: "CSV के रूप में निर्यात करें",
      exportPDF: "PDF के रूप में निर्यात करें",
      welcome: "Valora में आपका स्वागत है",
      manage: "अपने {{module}} रिकॉर्ड प्रबंधित करें",
      new: "नया {{module}}",
      success: "सफलता",
      error: "त्रुटि",
      operationFailed: "ऑपरेशन विफल",
      validationError: "सत्यापन त्रुटि",
      uniqueViolation: "यह रिकॉर्ड सहेजा नहीं जा सकता क्योंकि यह एक मौजूदा प्रविष्टि को डुप्लिकेट करता है।",
      uniqueViolationHint: "डेटा अखंडता सुनिश्चित करने के लिए कृपया अद्वितीय फ़ील्ड संशोधित करें।",
      id: "आईडी",
      createdAt: "बनाया गया",
      updatedAt: "अद्यतन किया गया",
      registration: "पंजीकरण",
      version: "संस्करण",
      select: "चुनें",
      unique: "अद्वितीय",
      searchId: "आईडी खोजें...",
      filterBy: "द्वारा फ़िल्टर करें...",
      enter: "{{field}} दर्ज करें",
      required: "{{field}} आवश्यक है",
      maxLength: "अधिकतम लंबाई {{length}} है",
      all: "सभी",
      page: "पृष्ठ",
      previous: "पिछला",
      next: "अगला",
      fields: {
        name: "नाम",
        firstName: "पहला नाम",
        lastName: "अंतिम नाम",
        email: "ईमेल",
        phone: "फ़ोन",
        dateOfBirth: "जन्म तिथि",
        gender: "लिंग",
        address: "पता",
        city: "शहर",
        state: "राज्य",
        zip: "पिन कोड",
        country: "देश",
        status: "स्थिति",
        description: "विवरण",
        notes: "टिप्पणियाँ",
        code: "कोड",
        type: "प्रकार"
      }
    },
    patient: {
      fields: {
        firstName: "पहला नाम",
        lastName: "अंतिम नाम",
        dateOfBirth: "जन्म तिथि",
        gender: "लिंग",
        email: "ईमेल",
        phone: "फ़ोन",
        notes: "चिकित्सा नोट्स",
        patientCode: "रोगी कोड"
      }
    },
    doctor: {
      fields: {
        name: "डॉक्टर का नाम",
        specialization: "विशेषज्ञता",
        licenseNumber: "लाइसेंस नंबर",
        phone: "फ़ोन",
        email: "ईमेल",
        schedule: "अनुसूची"
      }
    },
    nav: {
      main: "मुख्य",
      platform: "प्लेटफ़ॉर्म",
      doctors: "डॉक्टर",
      patients: "मरीज",
      studio: "स्टूडियो",
      systemLogs: "सिस्टम लॉग",
      aiDashboard: "एआई डैशबोर्ड",
      permissions: "अनुमतियां",
      appearance: "उपस्थिति",
      language: "भाषा",
      currency: "मुद्रा"
    }
  },
  'zh-CN': {
    common: {
      save: "保存",
      cancel: "取消",
      edit: "编辑",
      delete: "删除",
      view: "查看",
      create: "创建",
      update: "更新",
      backToList: "返回列表",
      loading: "加载中...",
      noRecords: "未找到记录。",
      filters: "筛选",
      active: "有效",
      actions: "操作",
      export: "导出数据",
      exportCSV: "导出为 CSV",
      exportPDF: "导出为 PDF",
      welcome: "欢迎来到 Valora",
      manage: "管理您的 {{module}} 记录",
      new: "新建 {{module}}",
      success: "成功",
      error: "错误",
      operationFailed: "操作失败",
      validationError: "验证错误",
      uniqueViolation: "无法保存此记录，因为它与现有条目重复。",
      uniqueViolationHint: "请修改唯一字段以确保数据完整性。",
      id: "ID",
      createdAt: "创建时间",
      updatedAt: "更新时间"
    },
    nav: {
      main: "主页",
      platform: "平台",
      doctors: "医生",
      patients: "患者",
      studio: "工作室",
      systemLogs: "系统日志",
      aiDashboard: "AI 仪表板",
      permissions: "权限",
      appearance: "外观",
      language: "语言",
      currency: "货币"
    }
  },
  ar: {
    common: {
      save: "حفظ",
      cancel: "إلغاء",
      edit: "تعديل",
      delete: "حذف",
      view: "عرض",
      create: "إنشاء",
      update: "تحديث",
      backToList: "العودة للقائمة",
      loading: "جار التحميل...",
      noRecords: "لا توجد سجلات.",
      filters: "تصفية",
      active: "نشط",
      actions: "إجراءات",
      export: "تصدير البيانات",
      exportCSV: "تصدير كـ CSV",
      exportPDF: "تصدير كـ PDF",
      welcome: "مرحبًا بك في Valora",
      manage: "إدارة سجلات {{module}}",
      new: "{{module}} جديد",
      success: "نجاح",
      error: "خطأ",
      operationFailed: "فشلت العملية",
      validationError: "خطأ في التحقق",
      uniqueViolation: "لا يمكن حفظ هذا السجل لأنه يكرر إدخالاً موجوداً.",
      uniqueViolationHint: "يرجى تعديل الحقول الفريدة لضمان سلامة البيانات.",
      id: "المعرف",
      createdAt: "تم الإنشاء في",
      updatedAt: "تم التحديث في"
    },
    nav: {
      main: "الرئيسية",
      platform: "المنصة",
      doctors: "الأطباء",
      patients: "المرضى",
      studio: "الاستوديو",
      systemLogs: "سجلات النظام",
      aiDashboard: "لوحة تحكم الذكاء الاصطناعي",
      permissions: "الأذونات",
      appearance: "المظهر",
      language: "اللغة",
      currency: "العملة"
    }
  },
  sw: {
    common: {
      save: "Hifadhi",
      cancel: "Ghairi",
      edit: "Hariri",
      delete: "Futa",
      view: "Tazama",
      create: "Unda",
      update: "Sasisha",
      backToList: "Rudi kwenye Orodha",
      loading: "Inapakia...",
      noRecords: "Hakuna rekodi zilizopatikana.",
      filters: "Vichungi",
      active: "Hai",
      actions: "Vitendo",
      export: "Hamisha Data",
      exportCSV: "Hamisha kama CSV",
      exportPDF: "Hamisha kama PDF",
      welcome: "Karibu Valora",
      manage: "Dhibiti rekodi zako za {{module}}",
      new: "{{module}} Mpya",
      success: "Mafanikio",
      error: "Hitilafu",
      operationFailed: "Operesheni Imeshindwa",
      validationError: "Hitilafu ya Uthibitishaji",
      uniqueViolation: "Rekodi hii haiwezi kuhifadhiwa kwa sababu inarudia ingizo lililopo.",
      uniqueViolationHint: "Tafadhali rekebisha sehemu za kipekee ili kuhakikisha uadilifu wa data.",
      id: "Kitambulisho",
      createdAt: "Imeundwa",
      updatedAt: "Imesasishwa"
    },
    nav: {
      main: "Kuu",
      platform: "Jukwaa",
      doctors: "Madaktari",
      patients: "Wagonjwa",
      studio: "Studio",
      systemLogs: "Kumbukumbu za Mfumo",
      aiDashboard: "Dashibodi ya AI",
      permissions: "Ruhusa",
      appearance: "Muonekano",
      language: "Lugha",
      currency: "Sarafu"
    }
  },
  bn: {
    common: {
      save: "সংরক্ষণ",
      cancel: "বাতিল",
      edit: "সম্পাদনা",
      delete: "মুছুন",
      view: "দেখুন",
      create: "তৈরি করুন",
      update: "আপডেট",
      backToList: "তালিকায় ফিরে যান",
      loading: "লোড হচ্ছে...",
      noRecords: "কোন রেকর্ড পাওয়া যায়নি।",
      filters: "ফিল্টার",
      active: "সক্রিয়",
      actions: "কর্ম",
      export: "ডেটা রপ্তানি",
      exportCSV: "CSV হিসাবে রপ্তানি",
      exportPDF: "PDF হিসাবে রপ্তানি",
      welcome: "Valora এ স্বাগতম",
      manage: "আপনার {{module}} রেকর্ড পরিচালনা করুন",
      new: "নতুন {{module}}",
      success: "সফলতা",
      error: "ত্রুটি",
      operationFailed: "অপারেশন ব্যর্থ হয়েছে",
      validationError: "বৈধকরণ ত্রুটি",
      uniqueViolation: "এই রেকর্ডটি সংরক্ষণ করা যাবে না কারণ এটি একটি বিদ্যমান এন্ট্রির প্রতিলিপি।",
      uniqueViolationHint: "ডেটা অখণ্ডতা নিশ্চিত করতে দয়া করে অনন্য ক্ষেত্রগুলি সংশোধন করুন।",
      id: "আইডি",
      createdAt: "তৈরি হয়েছে",
      updatedAt: "আপডেট হয়েছে"
    },
    nav: {
      main: "প্রধান",
      platform: "প্ল্যাটফর্ম",
      doctors: "ডাক্তার",
      patients: "রোগী",
      studio: "স্টুডিও",
      systemLogs: "সিস্টেম লগ",
      aiDashboard: "এআই ড্যাশবোর্ড",
      permissions: "অনুমতি",
      appearance: "চেহারা",
      language: "ভাষা",
      currency: "মুদ্রা"
    }
  },
  ru: {
    common: {
      save: "Сохранить",
      cancel: "Отмена",
      edit: "Изменить",
      delete: "Удалить",
      view: "Просмотр",
      create: "Создать",
      update: "Обновить",
      backToList: "Назад к списку",
      loading: "Загрузка...",
      noRecords: "Записи не найдены.",
      filters: "Фильтры",
      active: "Активный",
      actions: "Действия",
      export: "Экспорт данных",
      exportCSV: "Экспорт в CSV",
      exportPDF: "Экспорт в PDF",
      welcome: "Добро пожаловать в Valora",
      manage: "Управление записями {{module}}",
      new: "Новый {{module}}",
      success: "Успех",
      error: "Ошибка",
      operationFailed: "Операция не удалась",
      validationError: "Ошибка валидации",
      uniqueViolation: "Эта запись не может быть сохранена, так как она дублирует существующую.",
      uniqueViolationHint: "Пожалуйста, измените уникальные поля для обеспечения целостности данных.",
      id: "ID",
      createdAt: "Создано",
      updatedAt: "Обновлено",
      registration: "Регистрация",
      version: "Версия",
      select: "Выбрать",
      unique: "Уникальный",
      searchId: "Поиск ID...",
      filterBy: "Фильтровать по...",
      enter: "Введите {{field}}",
      required: "Поле {{field}} обязательно",
      maxLength: "Максимальная длина {{length}}",
      all: "Все",
      page: "Страница",
      previous: "Назад",
      next: "Вперед",
      fields: {
        name: "Имя",
        firstName: "Имя",
        lastName: "Фамилия",
        email: "Email",
        phone: "Телефон",
        dateOfBirth: "Дата рождения",
        gender: "Пол",
        address: "Адрес",
        city: "Город",
        state: "Область/Штат",
        zip: "Индекс",
        country: "Страна",
        status: "Статус",
        description: "Описание",
        notes: "Заметки",
        code: "Код",
        type: "Тип"
      }
    },
    patient: {
      fields: {
        firstName: "Имя",
        lastName: "Фамилия",
        dateOfBirth: "Дата рождения",
        gender: "Пол",
        email: "Email",
        phone: "Телефон",
        notes: "Медицинские записи",
        patientCode: "Код пациента"
      }
    },
    doctor: {
      fields: {
        name: "Имя врача",
        specialization: "Специализация",
        licenseNumber: "Номер лицензии",
        phone: "Телефон",
        email: "Email",
        schedule: "Расписание"
      }
    },
    nav: {
      main: "Главная",
      platform: "Платформа",
      doctors: "Врачи",
      patients: "Пациенты",
      studio: "Студия",
      systemLogs: "Системные логи",
      aiDashboard: "AI Панель",
      permissions: "Права доступа",
      appearance: "Внешний вид",
      language: "Язык",
      currency: "Валюта"
    }
  },
  ja: {
    common: {
      save: "保存",
      cancel: "キャンセル",
      edit: "編集",
      delete: "削除",
      view: "表示",
      create: "作成",
      update: "更新",
      backToList: "リストに戻る",
      loading: "読み込み中...",
      noRecords: "レコードが見つかりません。",
      filters: "フィルター",
      active: "アクティブ",
      actions: "アクション",
      export: "データのエクスポート",
      exportCSV: "CSVとしてエクスポート",
      exportPDF: "PDFとしてエクスポート",
      welcome: "Valoraへようこそ",
      manage: "{{module}} レコードの管理",
      new: "新規 {{module}}",
      success: "成功",
      error: "エラー",
      operationFailed: "操作に失敗しました",
      validationError: "検証エラー",
      uniqueViolation: "このレコードは既存のエントリと重複しているため保存できません。",
      uniqueViolationHint: "データの整合性を確保するために、一意のフィールドを変更してください。",
      id: "ID",
      createdAt: "作成日時",
      updatedAt: "更新日時"
    },
    nav: {
      main: "メイン",
      platform: "プラットフォーム",
      doctors: "医師",
      patients: "患者",
      studio: "スタジオ",
      systemLogs: "システムログ",
      aiDashboard: "AIダッシュボード",
      permissions: "権限",
      appearance: "外観",
      language: "言語",
      currency: "通貨"
    }
  },
  pt: {
    common: {
      save: "Salvar",
      cancel: "Cancelar",
      edit: "Editar",
      delete: "Excluir",
      view: "Visualizar",
      create: "Criar",
      update: "Atualizar",
      backToList: "Voltar para a lista",
      loading: "Carregando...",
      noRecords: "Nenhum registro encontrado.",
      filters: "Filtros",
      active: "Ativo",
      actions: "Ações",
      export: "Exportar Dados",
      exportCSV: "Exportar como CSV",
      exportPDF: "Exportar como PDF",
      welcome: "Bem-vindo ao Valora",
      manage: "Gerencie seus registros de {{module}}",
      new: "Novo {{module}}",
      success: "Sucesso",
      error: "Erro",
      operationFailed: "Falha na Operação",
      validationError: "Erro de Validação",
      uniqueViolation: "Este registro não pode ser salvo porque duplica uma entrada existente.",
      uniqueViolationHint: "Por favor, modifique os campos únicos para garantir a integridade dos dados.",
      id: "ID",
      createdAt: "Criado em",
      updatedAt: "Atualizado em"
    },
    nav: {
      main: "Principal",
      platform: "Plataforma",
      doctors: "Médicos",
      patients: "Pacientes",
      studio: "Estúdio",
      systemLogs: "Logs do Sistema",
      aiDashboard: "Painel de IA",
      permissions: "Permissões",
      appearance: "Aparência",
      language: "Idioma",
      currency: "Moeda"
    }
  },
  it: {
    common: {
      save: "Salva",
      cancel: "Annulla",
      edit: "Modifica",
      delete: "Elimina",
      view: "Visualizza",
      create: "Crea",
      update: "Aggiorna",
      backToList: "Torna alla lista",
      loading: "Caricamento...",
      noRecords: "Nessun record trovato.",
      filters: "Filtri",
      active: "Attivo",
      actions: "Azioni",
      export: "Esporta Dati",
      exportCSV: "Esporta come CSV",
      exportPDF: "Esporta come PDF",
      welcome: "Benvenuto in Valora",
      manage: "Gestisci i tuoi record {{module}}",
      new: "Nuovo {{module}}",
      success: "Successo",
      error: "Errore",
      operationFailed: "Operazione Fallita",
      validationError: "Errore di Convalida",
      uniqueViolation: "Questo record non può essere salvato perché duplica una voce esistente.",
      uniqueViolationHint: "Modifica i campi univoci per garantire l'integrità dei dati.",
      id: "ID",
      createdAt: "Creato il",
      updatedAt: "Aggiornato il"
    },
    nav: {
      main: "Principale",
      platform: "Piattaforma",
      doctors: "Medici",
      patients: "Pazienti",
      studio: "Studio",
      systemLogs: "Log di Sistema",
      aiDashboard: "Dashboard IA",
      permissions: "Permessi",
      appearance: "Aspetto",
      language: "Lingua",
      currency: "Valuta"
    }
  },
  nl: {
    common: {
      save: "Opslaan",
      cancel: "Annuleren",
      edit: "Bewerken",
      delete: "Verwijderen",
      view: "Bekijken",
      create: "Aanmaken",
      update: "Bijwerken",
      backToList: "Terug naar lijst",
      loading: "Laden...",
      noRecords: "Geen records gevonden.",
      filters: "Filters",
      active: "Actief",
      actions: "Acties",
      export: "Gegevens Exporteren",
      exportCSV: "Exporteren als CSV",
      exportPDF: "Exporteren als PDF",
      welcome: "Welkom bij Valora",
      manage: "Beheer uw {{module}} records",
      new: "Nieuwe {{module}}",
      success: "Succes",
      error: "Fout",
      operationFailed: "Bewerking Mislukt",
      validationError: "Validatiefout",
      uniqueViolation: "Dit record kan niet worden opgeslagen omdat het een bestaand item dupliceert.",
      uniqueViolationHint: "Wijzig de unieke velden om de gegevensintegriteit te waarborgen.",
      id: "ID",
      createdAt: "Aangemaakt op",
      updatedAt: "Bijgewerkt op"
    },
    nav: {
      main: "Hoofd",
      platform: "Platform",
      doctors: "Artsen",
      patients: "Patiënten",
      studio: "Studio",
      systemLogs: "Systeemlogboeken",
      aiDashboard: "AI Dashboard",
      permissions: "Rechten",
      appearance: "Uiterlijk",
      language: "Taal",
      currency: "Valuta"
    }
  },
  tr: {
    common: {
      save: "Kaydet",
      cancel: "İptal",
      edit: "Düzenle",
      delete: "Sil",
      view: "Görüntüle",
      create: "Oluştur",
      update: "Güncelle",
      backToList: "Listeye Dön",
      loading: "Yükleniyor...",
      noRecords: "Kayıt bulunamadı.",
      filters: "Filtreler",
      active: "Aktif",
      actions: "İşlemler",
      export: "Veri Dışa Aktar",
      exportCSV: "CSV olarak dışa aktar",
      exportPDF: "PDF olarak dışa aktar",
      welcome: "Valora'ya Hoşgeldiniz",
      manage: "{{module}} kayıtlarınızı yönetin",
      new: "Yeni {{module}}",
      success: "Başarılı",
      error: "Hata",
      operationFailed: "İşlem Başarısız",
      validationError: "Doğrulama Hatası",
      uniqueViolation: "Bu kayıt, mevcut bir girişle çakıştığı için kaydedilemiyor.",
      uniqueViolationHint: "Veri bütünlüğünü sağlamak için lütfen benzersiz alanları değiştirin.",
      id: "ID",
      createdAt: "Oluşturulma Tarihi",
      updatedAt: "Güncellenme Tarihi"
    },
    nav: {
      main: "Ana",
      platform: "Platform",
      doctors: "Doktorlar",
      patients: "Hastalar",
      studio: "Stüdyo",
      systemLogs: "Sistem Günlükleri",
      aiDashboard: "Yapay Zeka Paneli",
      permissions: "İzinler",
      appearance: "Görünüm",
      language: "Dil",
      currency: "Para Birimi"
    }
  },
  sv: {
    common: { save: "Spara", cancel: "Avbryt", edit: "Redigera", delete: "Ta bort", view: "Visa", create: "Skapa", update: "Uppdatera", backToList: "Tillbaka", loading: "Laddar...", noRecords: "Inga poster.", filters: "Filter", active: "Aktiv", actions: "Åtgärder", welcome: "Välkommen till Valora", id: "ID", createdAt: "Skapad", updatedAt: "Uppdaterad" },
    nav: { main: "Hem", platform: "Plattform", doctors: "Läkare", patients: "Patienter", studio: "Studio", systemLogs: "Systemloggar", aiDashboard: "AI Dashboard", permissions: "Behörigheter", appearance: "Utseende", language: "Språk", currency: "Valuta" }
  },
  pl: {
    common: { save: "Zapisz", cancel: "Anuluj", edit: "Edytuj", delete: "Usuń", view: "Widok", create: "Utwórz", update: "Aktualizuj", backToList: "Powrót", loading: "Ładowanie...", noRecords: "Brak rekordów.", filters: "Filtry", active: "Aktywny", actions: "Akcje", welcome: "Witamy w Valora", id: "ID", createdAt: "Utworzono", updatedAt: "Zaktualizowano" },
    nav: { main: "Główny", platform: "Platforma", doctors: "Lekarze", patients: "Pacjenci", studio: "Studio", systemLogs: "Logi systemowe", aiDashboard: "Pulpit AI", permissions: "Uprawnienia", appearance: "Wygląd", language: "Język", currency: "Waluta" }
  },
  uk: {
    common: { save: "Зберегти", cancel: "Скасувати", edit: "Редагувати", delete: "Видалити", view: "Перегляд", create: "Створити", update: "Оновити", backToList: "Назад", loading: "Завантаження...", noRecords: "Записів не знайдено.", filters: "Фільтри", active: "Активний", actions: "Дії", welcome: "Ласкаво просимо в Valora", id: "ID", createdAt: "Створено", updatedAt: "Оновлено" },
    nav: { main: "Головна", platform: "Платформа", doctors: "Лікарі", patients: "Пацієнти", studio: "Студія", systemLogs: "Системні логи", aiDashboard: "AI Панель", permissions: "Дозволи", appearance: "Вигляд", language: "Мова", currency: "Валюта" }
  },
  cs: {
    common: { save: "Uložit", cancel: "Zrušit", edit: "Upravit", delete: "Smazat", view: "Zobrazit", create: "Vytvořit", update: "Aktualizovat", backToList: "Zpět", loading: "Načítání...", noRecords: "Nenalezeny žádné záznamy.", filters: "Filtry", active: "Aktivní", actions: "Akce", welcome: "Vítejte v Valora", id: "ID", createdAt: "Vytvořeno", updatedAt: "Aktualizováno" },
    nav: { main: "Hlavní", platform: "Platforma", doctors: "Lékaři", patients: "Pacienti", studio: "Studio", systemLogs: "Systémové protokoly", aiDashboard: "AI Dashboard", permissions: "Oprávnění", appearance: "Vzhled", language: "Jazyk", currency: "Měna" }
  },
  ro: {
    common: { save: "Salvează", cancel: "Anulează", edit: "Editează", delete: "Șterge", view: "Vizualizare", create: "Creează", update: "Actualizează", backToList: "Înapoi la listă", loading: "Se încarcă...", noRecords: "Nu au fost găsite înregistrări.", filters: "Filtre", active: "Activ", actions: "Acțiuni", welcome: "Bun venit la Valora", id: "ID", createdAt: "Creat la", updatedAt: "Actualizat la" },
    nav: { main: "Principal", platform: "Platformă", doctors: "Medici", patients: "Pacienți", studio: "Studio", systemLogs: "Jurnale de sistem", aiDashboard: "Tablou de bord AI", permissions: "Permisiuni", appearance: "Aspect", language: "Limbă", currency: "Monedă" }
  },
  el: {
    common: { save: "Αποθήκευση", cancel: "Ακύρωση", edit: "Επεξεργασία", delete: "Διαγραφή", view: "Προβολή", create: "Δημιουργία", update: "Ενημέρωση", backToList: "Πίσω", loading: "Φόρτωση...", noRecords: "Δεν βρέθηκαν εγγραφές.", filters: "Φίλτρα", active: "Ενεργό", actions: "Ενέργειες", welcome: "Καλώς ήρθατε στο Valora", id: "ID", createdAt: "Δημιουργήθηκε", updatedAt: "Ενημερώθηκε" },
    nav: { main: "Κύρια", platform: "Πλατφόρμα", doctors: "Γιατροί", patients: "Ασθενείς", studio: "Στούντιο", systemLogs: "Αρχεία καταγραφής", aiDashboard: "AI Dashboard", permissions: "Δικαιώματα", appearance: "Εμφάνιση", language: "Γλώσσα", currency: "Νόμισμα" }
  },
  ko: {
    common: { save: "저장", cancel: "취소", edit: "편집", delete: "삭제", view: "보기", create: "생성", update: "업데이트", backToList: "목록으로", loading: "로딩 중...", noRecords: "기록 없음.", filters: "필터", active: "활성", actions: "작업", welcome: "Valora에 오신 것을 환영합니다", id: "ID", createdAt: "생성일", updatedAt: "수정일" },
    nav: { main: "메인", platform: "플랫폼", doctors: "의사", patients: "환자", studio: "스튜디오", systemLogs: "시스템 로그", aiDashboard: "AI 대시보드", permissions: "권한", appearance: "외관", language: "언어", currency: "통화" }
  },
  vi: {
    common: { save: "Lưu", cancel: "Hủy", edit: "Sửa", delete: "Xóa", view: "Xem", create: "Tạo", update: "Cập nhật", backToList: "Quay lại", loading: "Đang tải...", noRecords: "Không có dữ liệu.", filters: "Bộ lọc", active: "Hoạt động", actions: "Hành động", welcome: "Chào mừng đến với Valora", id: "ID", createdAt: "Đã tạo", updatedAt: "Đã cập nhật" },
    nav: { main: "Chính", platform: "Nền tảng", doctors: "Bác sĩ", patients: "Bệnh nhân", studio: "Studio", systemLogs: "Nhật ký hệ thống", aiDashboard: "Bảng điều khiển AI", permissions: "Quyền", appearance: "Giao diện", language: "Ngôn ngữ", currency: "Tiền tệ" }
  },
  th: {
    common: { save: "บันทึก", cancel: "ยกเลิก", edit: "แก้ไข", delete: "ลบ", view: "ดู", create: "สร้าง", update: "อัปเดต", backToList: "กลับไปที่รายการ", loading: "กำลังโหลด...", noRecords: "ไม่พบข้อมูล", filters: "ตัวกรอง", active: "ใช้งานอยู่", actions: "การกระทำ", welcome: "ยินดีต้อนรับสู่ Valora", id: "ID", createdAt: "สร้างเมื่อ", updatedAt: "อัปเดตเมื่อ" },
    nav: { main: "หน้าหลัก", platform: "แพลตฟอร์ม", doctors: "แพทย์", patients: "ผู้ป่วย", studio: "สตูดิโอ", systemLogs: "บันทึกระบบ", aiDashboard: "แดชบอร์ด AI", permissions: "สิทธิ์", appearance: "รูปลักษณ์", language: "ภาษา", currency: "สกุลเงิน" }
  },
  id: {
    common: { save: "Simpan", cancel: "Batal", edit: "Edit", delete: "Hapus", view: "Lihat", create: "Buat", update: "Perbarui", backToList: "Kembali", loading: "Memuat...", noRecords: "Tidak ada data.", filters: "Filter", active: "Aktif", actions: "Tindakan", welcome: "Selamat datang di Valora", id: "ID", createdAt: "Dibuat", updatedAt: "Diperbarui" },
    nav: { main: "Utama", platform: "Platform", doctors: "Dokter", patients: "Pasien", studio: "Studio", systemLogs: "Log Sistem", aiDashboard: "Dasbor AI", permissions: "Izin", appearance: "Tampilan", language: "Bahasa", currency: "Mata Uang" }
  },
  tl: {
    common: { save: "I-save", cancel: "Kanselahin", edit: "I-edit", delete: "Tanggalin", view: "Tingnan", create: "Lumikha", update: "I-update", backToList: "Bumalik", loading: "Naglo-load...", noRecords: "Walang nahanap na tala.", filters: "Mga Filter", active: "Aktibo", actions: "Mga Aksyon", welcome: "Maligayang pagdating sa Valora", id: "ID", createdAt: "Nilikha noong", updatedAt: "Na-update noong" },
    nav: { main: "Pangunahin", platform: "Platform", doctors: "Mga Doktor", patients: "Mga Pasyente", studio: "Studio", systemLogs: "System Logs", aiDashboard: "AI Dashboard", permissions: "Mga Pahintulot", appearance: "Hitsura", language: "Wika", currency: "Pera" }
  },
  ur: {
    common: { save: "محفوظ کریں", cancel: "منسوخ کریں", edit: "ترمیم", delete: "حذف کریں", view: "دیکھیں", create: "تخلیق کریں", update: "اپ ڈیٹ", backToList: "فہرست پر واپس جائیں", loading: "لوڈ ہو رہا ہے...", noRecords: "کوئی ریکارڈ نہیں ملا۔", filters: "فلٹرز", active: "فعال", actions: "عمل", welcome: "Valora میں خوش آمدید", id: "آئی ڈی", createdAt: "تخلیق شدہ", updatedAt: "اپ ڈیٹ شدہ" },
    nav: { main: "مین", platform: "پلیٹ فارم", doctors: "ڈاکٹرز", patients: "مریض", studio: "اسٹوڈیو", systemLogs: "سسٹم لاگز", aiDashboard: "AI ڈیش بورڈ", permissions: "اجازتیں", appearance: "ظاہری شکل", language: "زبان", currency: "کرنسی" }
  },
  ta: {
    common: { save: "சேமி", cancel: "ரத்துசெய்", edit: "திருத்து", delete: "அழி", view: "பார்", create: "உருவாக்கு", update: "புதுப்பி", backToList: "பட்டியலுக்குத் திரும்பு", loading: "ஏற்றுகிறது...", noRecords: "பதிவுகள் இல்லை.", filters: "வடிப்பான்கள்", active: "செயலில்", actions: "செயல்கள்", welcome: "Valora க்கு வரவேற்கிறோம்", id: "ID", createdAt: "உருவாக்கப்பட்டது", updatedAt: "புதுப்பிக்கப்பட்டது" },
    nav: { main: "முதன்மை", platform: "மேடை", doctors: "மருத்துவர்கள்", patients: "நோயாளிகள்", studio: "ஸ்டுடியோ", systemLogs: "கணினி பதிவுகள்", aiDashboard: "AI டாஷ்போர்டு", permissions: "அனுமதிகள்", appearance: "தோற்றம்", language: "மொழி", currency: "நாணயம்" }
  },
  te: {
    common: { save: "సేవ్ చేయి", cancel: "రద్దు చేయి", edit: "సవరించు", delete: "తొలగించు", view: "చూడు", create: "సృష్టించు", update: "నవీకరించు", backToList: "జాబితాకు వెళ్ళు", loading: "లోడ్ అవుతోంది...", noRecords: "రికార్డులు కనుగొనబడలేదు.", filters: "ఫిల్టర్లు", active: "యాక్టివ్", actions: "చర్యలు", welcome: "Valora కు స్వాగతం", id: "ID", createdAt: "సృష్టించబడింది", updatedAt: "నవీకరించబడింది" },
    nav: { main: "ప్రధాన", platform: "ప్లాట్‌ఫారమ్", doctors: "వైద్యులు", patients: "రోగులు", studio: "స్టూడియో", systemLogs: "సిస్టమ్ లాగ్స్", aiDashboard: "AI డాష్‌బోర్డ్", permissions: "అనుమతులు", appearance: "స్వరూపం", language: "భాష", currency: "కరెన్సీ" }
  },
  mr: {
    common: { save: "जतन करा", cancel: "रद्द करा", edit: "संपादित करा", delete: "हटवा", view: "पहा", create: "तयार करा", update: "अद्यतन करा", backToList: "यादीवर परत जा", loading: "लोड होत आहे...", noRecords: "कोणतेही रेकॉर्ड सापडले नाहीत.", filters: "फिल्टर", active: "सक्रिय", actions: "क्रिया", welcome: "Valora मध्ये आपले स्वागत आहे", id: "ID", createdAt: "तयार केले", updatedAt: "अद्यतनित केले" },
    nav: { main: "मुख्य", platform: "प्लॅटफॉर्म", doctors: "डॉक्टर", patients: "रुग्ण", studio: "स्टुडिओ", systemLogs: "सिस्टम लॉग", aiDashboard: "AI डॅशबोर्ड", permissions: "परवानग्या", appearance: "दिसणे", language: "भाषा", currency: "चलन" }
  },
  'en-GB': {
    common: { save: "Save", cancel: "Cancel", edit: "Edit", delete: "Delete", view: "View", create: "Create", update: "Update", backToList: "Back to List", loading: "Loading...", noRecords: "No records found.", filters: "Filters", active: "Active", actions: "Actions", welcome: "Welcome to Valora", id: "ID", createdAt: "Created At", updatedAt: "Updated At" },
    nav: { main: "Main", platform: "Platform", doctors: "Doctors", patients: "Patients", studio: "Studio", systemLogs: "System Logs", aiDashboard: "AI Dashboard", permissions: "Permissions", appearance: "Appearance", language: "Language", currency: "Currency" }
  },
  no: {
    common: { save: "Lagre", cancel: "Avbryt", edit: "Rediger", delete: "Slett", view: "Vis", create: "Opprett", update: "Oppdater", backToList: "Tilbake", loading: "Laster...", noRecords: "Ingen poster.", filters: "Filter", active: "Aktiv", actions: "Handlinger", welcome: "Velkommen til Valora", id: "ID", createdAt: "Opprettet", updatedAt: "Oppdatert" },
    nav: { main: "Hjem", platform: "Plattform", doctors: "Leger", patients: "Pasienter", studio: "Studio", systemLogs: "Systemlogger", aiDashboard: "AI Dashboard", permissions: "Tillatelser", appearance: "Utseende", language: "Språk", currency: "Valuta" }
  },
  da: {
    common: { save: "Gem", cancel: "Annuller", edit: "Rediger", delete: "Slet", view: "Vis", create: "Opret", update: "Opdater", backToList: "Tilbage", loading: "Indlæser...", noRecords: "Ingen poster.", filters: "Filtre", active: "Aktiv", actions: "Handlinger", welcome: "Velkommen til Valora", id: "ID", createdAt: "Oprettet", updatedAt: "Opdateret" },
    nav: { main: "Hjem", platform: "Platform", doctors: "Læger", patients: "Patienter", studio: "Studio", systemLogs: "Systemlogger", aiDashboard: "AI Dashboard", permissions: "Tilladelser", appearance: "Udseende", language: "Sprog", currency: "Valuta" }
  },
  fi: {
    common: { save: "Tallenna", cancel: "Peruuta", edit: "Muokkaa", delete: "Poista", view: "Näytä", create: "Luo", update: "Päivitä", backToList: "Takaisin", loading: "Ladataan...", noRecords: "Ei tietueita.", filters: "Suodattimet", active: "Aktiivinen", actions: "Toiminnot", welcome: "Tervetuloa Valora", id: "ID", createdAt: "Luotu", updatedAt: "Päivitetty" },
    nav: { main: "Etusivu", platform: "Alusta", doctors: "Lääkärit", patients: "Potilaat", studio: "Studio", systemLogs: "Järjestelmälokit", aiDashboard: "AI Kojelauta", permissions: "Oikeudet", appearance: "Ulkoasu", language: "Kieli", currency: "Valuutta" }
  },
  hu: {
    common: { save: "Mentés", cancel: "Mégse", edit: "Szerkesztés", delete: "Törlés", view: "Megtekintés", create: "Létrehozás", update: "Frissítés", backToList: "Vissza", loading: "Betöltés...", noRecords: "Nincs találat.", filters: "Szűrők", active: "Aktív", actions: "Műveletek", welcome: "Üdvözli a Valora", id: "ID", createdAt: "Létrehozva", updatedAt: "Frissítve" },
    nav: { main: "Főoldal", platform: "Platform", doctors: "Orvosok", patients: "Betegek", studio: "Stúdió", systemLogs: "Rendszernaplók", aiDashboard: "AI Irányítópult", permissions: "Jogosultságok", appearance: "Megjelenés", language: "Nyelv", currency: "Pénznem" }
  },
  'zh-TW': {
    common: { save: "儲存", cancel: "取消", edit: "編輯", delete: "刪除", view: "檢視", create: "建立", update: "更新", backToList: "返回列表", loading: "載入中...", noRecords: "找不到記錄。", filters: "篩選", active: "啟用", actions: "動作", welcome: "歡迎來到 Valora", id: "ID", createdAt: "建立於", updatedAt: "更新於" },
    nav: { main: "主頁", platform: "平台", doctors: "醫生", patients: "病患", studio: "工作室", systemLogs: "系統記錄", aiDashboard: "AI 儀表板", permissions: "權限", appearance: "外觀", language: "語言", currency: "貨幣" }
  },
  ms: {
    common: { save: "Simpan", cancel: "Batal", edit: "Edit", delete: "Padam", view: "Lihat", create: "Cipta", update: "Kemaskini", backToList: "Kembali", loading: "Memuatkan...", noRecords: "Tiada rekod.", filters: "Penapis", active: "Aktif", actions: "Tindakan", welcome: "Selamat datang ke Valora", id: "ID", createdAt: "Dicipta", updatedAt: "Dikemaskini" },
    nav: { main: "Utama", platform: "Platform", doctors: "Doktor", patients: "Pesakit", studio: "Studio", systemLogs: "Log Sistem", aiDashboard: "Papan Pemuka AI", permissions: "Kebenaran", appearance: "Penampilan", language: "Bahasa", currency: "Mata Wang" }
  },
  gu: {
    common: { save: "સાચવો", cancel: "રદ કરો", edit: "ફેરફાર", delete: "કાઢી નાખો", view: "જુઓ", create: "બનાવો", update: "અપડેટ", backToList: "પાછા જાઓ", loading: "લોડ કરી રહ્યું છે...", noRecords: "કોઈ રેકોર્ડ મળ્યો નથી.", filters: "ફિલ્ટર્સ", active: "સક્રિય", actions: "ક્રિયાઓ", welcome: "Valora માં સ્વાગત છે", id: "ID", createdAt: "બનાવ્યું", updatedAt: "અપડેટ કર્યું" },
    nav: { main: "મુખ્ય", platform: "પ્લેટફોર્મ", doctors: "ડોકટરો", patients: "દર્દીઓ", studio: "સ્ટુડિયો", systemLogs: "સિસ્ટમ લોગ", aiDashboard: "AI ડેશબોર્ડ", permissions: "પરવાનગીઓ", appearance: "દેખાવ", language: "ભાષા", currency: "ચલણ" }
  },
  kn: {
    common: { save: "ಉಳಿಸಿ", cancel: "ರದ್ದುಮಾಡಿ", edit: "ತಿದ್ದು", delete: "ಅಳಿಸಿ", view: "ನೋಡಿ", create: "ರಚಿಸಿ", update: "ನವೀಕರಿಸಿ", backToList: "ಹಿಂದೆ", loading: "ಲೋಡ್ ಆಗುತ್ತಿದೆ...", noRecords: "ಯಾವುದೇ ದಾಖಲೆಗಳಿಲ್ಲ.", filters: "ಫಿಲ್ಟರ್‌ಗಳು", active: "ಸಕ್ರಿಯ", actions: "ಕ್ರಿಯೆಗಳು", welcome: "Valora ಗೆ ಸುಸ್ವಾಗತ", id: "ID", createdAt: "ರಚಿಸಲಾಗಿದೆ", updatedAt: "ನವೀಕರಿಸಲಾಗಿದೆ" },
    nav: { main: "ಮುಖ್ಯ", platform: "ಪ್ಲಾಟ್‌ಫಾರ್ಮ್", doctors: "ವೈದ್ಯರು", patients: "ರೋಗಿಗಳು", studio: "ಸ್ಟುಡಿಯೋ", systemLogs: "ಸಿಸ್ಟಮ್ ಲಾಗ್", aiDashboard: "AI ಡ್ಯಾಶ್‌ಬೋರ್ಡ್", permissions: "ಅನುಮತಿಗಳು", appearance: "ನೋಟ", language: "ಭಾಷೆ", currency: "ಕರೆನ್ಸಿ" }
  },
  ml: {
    common: { save: "സംരക്ഷിക്കുക", cancel: "റദ്ദാക്കുക", edit: "എഡിറ്റ്", delete: "നീക്കം", view: "കാണുക", create: "സൃഷ്ടിക്കുക", update: "അപ്ഡേറ്റ്", backToList: "തിരികെ", loading: "ലോഡുചെയ്യുന്നു...", noRecords: "രേഖകളൊന്നുമില്ല.", filters: "ഫിൽട്ടറുകൾ", active: "സജീവം", actions: "പ്രവർത്തനങ്ങൾ", welcome: "Valora-ലേക്ക് സ്വാഗതം", id: "ID", createdAt: "സൃഷ്ടിച്ചത്", updatedAt: "അപ്ഡേറ്റ് ചെയ്തത്" },
    nav: { main: "പ്രധാനം", platform: "പ്ലാറ്റ്‌ഫോം", doctors: "ഡോക്ടർമാർ", patients: "രോഗികൾ", studio: "സ്റ്റുഡിയോ", systemLogs: "സിസ്റ്റം ലോഗുകൾ", aiDashboard: "AI ഡാഷ്‌ബോർഡ്", permissions: "അനുമതികൾ", appearance: "രൂപം", language: "ഭാഷ", currency: "കറൻസി" }
  },
  pa: {
    common: { save: "ਸੰਭਾਲੋ", cancel: "ਰੱਦ ਕਰੋ", edit: "ਸੋਧੋ", delete: "ਹਟਾਓ", view: "ਵੇਖੋ", create: "ਬਣਾਓ", update: "ਅਪਡੇਟ", backToList: "ਵਾਪਸ", loading: "ਲੋਡ ਹੋ ਰਿਹਾ ਹੈ...", noRecords: "ਕੋਈ ਰਿਕਾਰਡ ਨਹੀਂ।", filters: "ਫਿਲਟਰ", active: "ਸਰਗਰਮ", actions: "ਕਾਰਵਾਈਆਂ", welcome: "Valora ਵਿੱਚ ਜੀ ਆਇਆਂ ਨੂੰ", id: "ID", createdAt: "ਬਣਾਇਆ", updatedAt: "ਅਪਡੇਟ ਕੀਤਾ" },
    nav: { main: "ਮੁੱਖ", platform: "ਪਲੇਟਫਾਰਮ", doctors: "ਡਾਕਟਰ", patients: "ਮਰੀਜ਼", studio: "ਸਟੂਡੀਓ", systemLogs: "ਸਿਸਟਮ ਲੌਗ", aiDashboard: "AI ਡੈਸ਼ਬੋਰਡ", permissions: "ਅਧਿਕਾਰ", appearance: "ਦਿੱਖ", language: "ਭਾਸ਼ਾ", currency: "ਮੁਦਰਾ" }
  },
  am: {
    common: { save: "አስቀምጥ", cancel: "ሰርዝ", edit: "አርትዕ", delete: "ሰርዝ", view: "ተመልከት", create: "ፍጠር", update: "አዘምን", backToList: "ተመለስ", loading: "በመጫን ላይ...", noRecords: "ምንም መዝገብ አልተገኘም", filters: "ማጣሪያዎች", active: "ንቁ", actions: "ድርጊቶች", welcome: "እንኳን ወደ Valora በደህና መጡ", id: "መለያ", createdAt: "የተፈጠረው", updatedAt: "የዘመነው" },
    nav: { main: "ዋና", platform: "መድረክ", doctors: "ሐኪሞች", patients: "ታካሚዎች", studio: "ስቱዲዮ", systemLogs: "የስርዓት ምዝግብ ማስታወሻዎች", aiDashboard: "AI ዳሽቦርድ", permissions: "ፈቃዶች", appearance: "መልክ", language: "ቋንቋ", currency: "ምንዛሬ" }
  },
  zu: {
    common: { save: "Gcina", cancel: "Khansela", edit: "Hlela", delete: "Susa", view: "Buka", create: "Dala", update: "Buyekeza", backToList: "Emuva", loading: "Iyalayisha...", noRecords: "Ayikho irekhodi.", filters: "Izihlungi", active: "Iyasebenza", actions: "Izinyathelo", welcome: "Uyemukelwa ku-Valora", id: "ID", createdAt: "Kudalwe", updatedAt: "Kubuyekeziwe" },
    nav: { main: "Okuyinhloko", platform: "Ipulatifomu", doctors: "Odokotela", patients: "Iziguli", studio: "Isitudiyo", systemLogs: "Amalogi Wesistimu", aiDashboard: "Ideshibhodi ye-AI", permissions: "Izimvume", appearance: "Ukubukeka", language: "Ulimi", currency: "Imali" }
  },
  af: {
    common: { save: "Stoor", cancel: "Kanselleer", edit: "Wysig", delete: "Skrap", view: "Besigtig", create: "Skep", update: "Opdateer", backToList: "Terug", loading: "Laai...", noRecords: "Geen rekords.", filters: "Filters", active: "Aktief", actions: "Aksies", welcome: "Welkom by Valora", id: "ID", createdAt: "Geskep", updatedAt: "Opgedateer" },
    nav: { main: "Tuis", platform: "Platform", doctors: "Dokters", patients: "Pasiënte", studio: "Ateljee", systemLogs: "Stelsellogs", aiDashboard: "AI Paneelbord", permissions: "Toestemmings", appearance: "Voorkoms", language: "Taal", currency: "Geldeenheid" }
  },
  ha: {
    common: { save: "Ajiye", cancel: "Soke", edit: "Gyara", delete: "Goge", view: "Duba", create: "Kira", update: "Sabunta", backToList: "Koma Baya", loading: "Ana Lodawa...", noRecords: "Ba a sami bayanai ba.", filters: "Matatun", active: "Mai Aiki", actions: "Ayyuka", welcome: "Barka da zuwa Valora", id: "ID", createdAt: "An Kirkira", updatedAt: "An Sabunta" },
    nav: { main: "Babba", platform: "Dandamali", doctors: "Likitoci", patients: "Marasa lafiya", studio: "Studio", systemLogs: "Bayanin Tsarin", aiDashboard: "AI Dashboard", permissions: "Izini", appearance: "Bayyanar", language: "Harshe", currency: "Kudin" }
  },
  yo: {
    common: { save: "Fipamọ", cancel: "Fagilee", edit: "Ṣatunkọ", delete: "Paarẹ", view: "Wo", create: "Ṣẹda", update: "Ṣe imudojuiwọn", backToList: "Pada", loading: "N kojọpọ...", noRecords: "Ko si igbasilẹ.", filters: "Ajọ", active: "Ti nṣiṣe lọwọ", actions: "Awọn iṣe", welcome: "Kaabo si Valora", id: "ID", createdAt: "Ti ṣẹda", updatedAt: "Ti ni imudojuiwọn" },
    nav: { main: "Akọkọ", platform: "Syeed", doctors: "Awọn dokita", patients: "Awọn alaisan", studio: "Studio", systemLogs: "Awọn akọọlẹ Eto", aiDashboard: "AI Dasibodu", permissions: "Awọn igbanilaaye", appearance: "Ifarahan", language: "Ede", currency: "Owo" }
  },
  ig: {
    common: { save: "Chekwaa", cancel: "Kagbuo", edit: "Dezie", delete: "Hichapụ", view: "Lelee", create: "Mepụta", update: "Melite", backToList: "Laghachi", loading: "Na-ebu...", noRecords: "Enweghị ndekọ.", filters: "Ihe nzacha", active: "Na-arụ ọrụ", actions: "Omume", welcome: "Nnọọ na Valora", id: "ID", createdAt: "Emepụtara", updatedAt: "Emelitere" },
    nav: { main: "Isi", platform: "Ikpo okwu", doctors: "Ndị dọkịta", patients: "Ndị ọrịa", studio: "Studio", systemLogs: "Ndekọ sistemụ", aiDashboard: "AI Dashboard", permissions: "Ikikere", appearance: "Ọdịdị", language: "Asụsụ", currency: "Ego" }
  },
};

export const SUPPORTED_LANGUAGES = [
  // Western Europe
  { code: 'en', label: 'English (US)' },
  { code: 'en-GB', label: 'English (UK)' },
  { code: 'de', label: 'Deutsch (German)' },
  { code: 'fr', label: 'Français (French)' },
  { code: 'es', label: 'Español (Spanish)' },
  { code: 'it', label: 'Italiano (Italian)' },
  { code: 'pt', label: 'Português (Portuguese)' },
  { code: 'nl', label: 'Nederlands (Dutch)' },
  
  // Northern Europe
  { code: 'sv', label: 'Svenska (Swedish)' },
  { code: 'no', label: 'Norsk (Norwegian)' },
  { code: 'da', label: 'Dansk (Danish)' },
  { code: 'fi', label: 'Suomi (Finnish)' },
  
  // Eastern Europe / Russia
  { code: 'ru', label: 'Русский (Russian)' },
  { code: 'pl', label: 'Polski (Polish)' },
  { code: 'uk', label: 'Українська (Ukrainian)' },
  { code: 'cs', label: 'Čeština (Czech)' },
  { code: 'hu', label: 'Magyar (Hungarian)' },
  { code: 'ro', label: 'Română (Romanian)' },
  { code: 'el', label: 'Ελληνικά (Greek)' },
  { code: 'tr', label: 'Türkçe (Turkish)' },

  // India
  { code: 'hi', label: 'हिन्दी (Hindi)' },
  { code: 'bn', label: 'বাংলা (Bengali)' },
  { code: 'te', label: 'తెలుగు (Telugu)' },
  { code: 'mr', label: 'मराठी (Marathi)' },
  { code: 'ta', label: 'தமிழ் (Tamil)' },
  { code: 'ur', label: 'اردو (Urdu)' },
  { code: 'gu', label: 'ગુજરાતી (Gujarati)' },
  { code: 'kn', label: 'ಕನ್ನಡ (Kannada)' },
  { code: 'ml', label: 'മലയാളം (Malayalam)' },
  { code: 'pa', label: 'ਪੰਜਾਬੀ (Punjabi)' },

  // Asia
  { code: 'zh-CN', label: '中文 (Chinese Simplified)' },
  { code: 'zh-TW', label: '中文 (Chinese Traditional)' },
  { code: 'ja', label: '日本語 (Japanese)' },
  { code: 'ko', label: '한국어 (Korean)' },
  { code: 'vi', label: 'Tiếng Việt (Vietnamese)' },
  { code: 'th', label: 'ไทย (Thai)' },
  { code: 'id', label: 'Bahasa Indonesia' },
  { code: 'ms', label: 'Bahasa Melayu' },
  { code: 'tl', label: 'Tagalog (Filipino)' },

  // Africa / Middle East
  { code: 'ar', label: 'العربية (Arabic)' },
  { code: 'sw', label: 'Kiswahili (Swahili)' },
  { code: 'am', label: 'አማርኛ (Amharic)' },
  { code: 'yo', label: 'Yorùbá' },
  { code: 'ig', label: 'Igbo' },
  { code: 'ha', label: 'Hausa' },
  { code: 'zu', label: 'isiZulu (Zulu)' },
  { code: 'af', label: 'Afrikaans' }
];

export const SUPPORTED_CURRENCIES = [
  // Major
  { code: 'USD', label: 'US Dollar', symbol: '$' },
  { code: 'EUR', label: 'Euro', symbol: '€' },
  { code: 'GBP', label: 'British Pound', symbol: '£' },
  { code: 'JPY', label: 'Japanese Yen', symbol: '¥' },
  { code: 'CNY', label: 'Chinese Yuan', symbol: '¥' },
  { code: 'INR', label: 'Indian Rupee', symbol: '₹' },
  
  // Europe
  { code: 'CHF', label: 'Swiss Franc', symbol: 'Fr' },
  { code: 'SEK', label: 'Swedish Krona', symbol: 'kr' },
  { code: 'NOK', label: 'Norwegian Krone', symbol: 'kr' },
  { code: 'DKK', label: 'Danish Krone', symbol: 'kr' },
  { code: 'PLN', label: 'Polish Złoty', symbol: 'zł' },
  { code: 'RUB', label: 'Russian Ruble', symbol: '₽' },
  { code: 'TRY', label: 'Turkish Lira', symbol: '₺' },
  { code: 'HUF', label: 'Hungarian Forint', symbol: 'Ft' },
  { code: 'CZK', label: 'Czech Koruna', symbol: 'Kč' },
  
  // Asia / Pacific
  { code: 'AUD', label: 'Australian Dollar', symbol: '$' },
  { code: 'CAD', label: 'Canadian Dollar', symbol: '$' },
  { code: 'HKD', label: 'Hong Kong Dollar', symbol: '$' },
  { code: 'SGD', label: 'Singapore Dollar', symbol: '$' },
  { code: 'KRW', label: 'South Korean Won', symbol: '₩' },
  { code: 'THB', label: 'Thai Baht', symbol: '฿' },
  { code: 'IDR', label: 'Indonesian Rupiah', symbol: 'Rp' },
  { code: 'MYR', label: 'Malaysian Ringgit', symbol: 'RM' },
  { code: 'PHP', label: 'Philippine Peso', symbol: '₱' },
  { code: 'VND', label: 'Vietnamese Dong', symbol: '₫' },
  { code: 'NZD', label: 'New Zealand Dollar', symbol: '$' },
  
  // India / South Asia
  { code: 'PKR', label: 'Pakistani Rupee', symbol: '₨' },
  { code: 'BDT', label: 'Bangladeshi Taka', symbol: '৳' },
  { code: 'LKR', label: 'Sri Lankan Rupee', symbol: 'Rs' },
  { code: 'NPR', label: 'Nepalese Rupee', symbol: 'Rs' },

  // Middle East / Africa
  { code: 'AED', label: 'UAE Dirham', symbol: 'د.إ' },
  { code: 'SAR', label: 'Saudi Riyal', symbol: '﷼' },
  { code: 'ILS', label: 'Israeli Shekel', symbol: '₪' },
  { code: 'EGP', label: 'Egyptian Pound', symbol: 'E£' },
  { code: 'ZAR', label: 'South African Rand', symbol: 'R' },
  { code: 'NGN', label: 'Nigerian Naira', symbol: '₦' },
  { code: 'KES', label: 'Kenyan Shilling', symbol: 'KSh' },
  { code: 'GHS', label: 'Ghanaian Cedi', symbol: '₵' },
  { code: 'MAD', label: 'Moroccan Dirham', symbol: 'DH' },

  // Americas
  { code: 'BRL', label: 'Brazilian Real', symbol: 'R$' },
  { code: 'MXN', label: 'Mexican Peso', symbol: '$' },
  { code: 'ARS', label: 'Argentine Peso', symbol: '$' },
  { code: 'COP', label: 'Colombian Peso', symbol: '$' },
  { code: 'CLP', label: 'Chilean Peso', symbol: '$' }
];
