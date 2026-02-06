# Documentation Index

Complete guide to PBIHoster documentation. Choose your path based on your role and needs.

## 🚀 Quick Navigation by Role

### I'm a User
- **Getting Started**: [README.md](README.md#quick-start) - Deploy and access PBIHoster
- **Managing Content**: [README.md](README.md#user-guide) - Create pages, manage users
- **Troubleshooting**: [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - Common issues and fixes
- **Security**: [SECURITY.md](SECURITY.md) - Understand security features

### I'm Deploying to Production
- **Deployment Guide**: [deployment/DEPLOYMENT.md](deployment/DEPLOYMENT.md) - Step-by-step setup
- **Security Checklist**: [SECURITY.md](SECURITY.md#-pre-production-checklist) - Pre-production requirements
- **Configuration**: [deployment/DEPLOYMENT.md](deployment/DEPLOYMENT.md#essential-security-settings) - Environment variables
- **Power BI Setup**: [deployment/DEPLOYMENT.md](deployment/DEPLOYMENT.md#power-bi-integration) - Azure AD and service principal config
- **Operations**: [TROUBLESHOOTING.md](TROUBLESHOOTING.md#operational-tasks) - Daily, weekly, monthly tasks

### I'm a Developer
- **Architecture**: [ARCHITECTURE.md](ARCHITECTURE.md) - System design and patterns
- **API Documentation**: [API.md](API.md) - REST endpoints and authentication
- **Database Schema**: [DATABASE.md](DATABASE.md) - LiteDB collections and queries
- **Contributing**: [CONTRIBUTING.md](CONTRIBUTING.md) - Development setup and code standards
- **Roadmap**: [ROADMAP.md](ROADMAP.md) - Feature plans and implementation timeline

### I'm Operating/Maintaining the System
- **Operations Guide**: [TROUBLESHOOTING.md](TROUBLESHOOTING.md#operational-tasks) - Daily/weekly/monthly tasks
- **Monitoring**: [TROUBLESHOOTING.md](TROUBLESHOOTING.md#monitoring-best-practices) - Metrics and alerting
- **Disaster Recovery**: [TROUBLESHOOTING.md](TROUBLESHOOTING.md#disaster-recovery) - Backup and restore
- **Troubleshooting**: [TROUBLESHOOTING.md](TROUBLESHOOTING.md#troubleshooting) - Common issues with solutions

---

## 📚 Documentation by Category

### Overview & Getting Started
| Document | Purpose | Audience |
|----------|---------|----------|
| [README.md](README.md) | High-level overview, quick start, feature overview | Everyone |
| [RELEASE_NOTES.md](documentation/RELEASE_NOTES.md) | Latest release highlights | Everyone |
| [CHANGELOG.md](CHANGELOG.md) | Detailed version history | Everyone |

### Deployment & Operations
| Document | Purpose | Audience |
|----------|---------|----------|
| [deployment/DEPLOYMENT.md](deployment/DEPLOYMENT.md) | Production deployment, Power BI setup, security config | DevOps, System Admins |
| [TROUBLESHOOTING.md](TROUBLESHOOTING.md) | Common issues, resolution steps, monitoring | System Admins, Operators |
| [SECURITY.md](SECURITY.md) | Security features, best practices, compliance | Security, System Admins |
| [deployment/RUNBOOK_LOGGING.md](deployment/RUNBOOK_LOGGING.md) | Logging, metrics, health checks | System Admins |

### Development
| Document | Purpose | Audience |
|----------|---------|----------|
| [CONTRIBUTING.md](CONTRIBUTING.md) | Development setup, code standards, PR process | Developers |
| [ARCHITECTURE.md](ARCHITECTURE.md) | System design, patterns, data models | Architects, Senior Devs |
| [API.md](API.md) | REST API endpoints, authentication, error handling | Backend Devs, Integrators |
| [DATABASE.md](DATABASE.md) | LiteDB schema, queries, optimization | Backend Devs, DBAs |
| [ROADMAP.md](ROADMAP.md) | Feature plans, implementation timeline | Product Managers, Developers |

---

## 🗺️ How Documents Connect

```
README.md (Entry point)
├─ Quick Start → deployment/DEPLOYMENT.md (production)
│  ├─ Power BI Setup
│  ├─ Security Config → SECURITY.md
│  └─ Operations → TROUBLESHOOTING.md
│
├─ Architecture → ARCHITECTURE.md (design details)
│  ├─ API Structure → API.md (endpoint docs)
│  ├─ Data Model → DATABASE.md (schema docs)
│  └─ Components → CONTRIBUTING.md (code structure)
│
├─ Development → CONTRIBUTING.md (setup & standards)
│  ├─ Code Standards
│  ├─ Testing & CI
│  └─ Workflow
│
└─ Feature Plans → ROADMAP.md (future direction)
   ├─ Phases 1-5
   ├─ Timeline
   └─ Contribution Opportunities
```

---

## 📖 Document Descriptions

### [README.md](README.md)
**Overview of PBIHoster for everyone**

- What is PBIHoster and who it's for
- Feature highlights and comparison
- Quick start for Docker and development
- Basic user guide (pages, users, themes)
- Links to detailed documentation

**Start here first!**

### [ARCHITECTURE.md](ARCHITECTURE.md)
**Complete system design and technical architecture**

- System overview and layered architecture
- Component descriptions and responsibilities
- Database schema and entity relationships
- Authentication and authorization model
- Power BI integration architecture
- Security layers and approach
- Configuration management
- Monitoring and observability

**Read this for:** Understanding how everything works together

### [API.md](API.md)
**Complete REST API reference documentation**

- Authentication via JWT Bearer tokens
- All endpoints organized by resource
- Request/response examples
- Error handling and status codes
- Rate limiting and pagination
- Correlation IDs for tracing

**Read this for:** Calling the API or building integrations

### [DATABASE.md](DATABASE.md)
**LiteDB database schema and usage**

- All collections (AppUser, Page, AuditLog, etc.)
- Entity fields and types
- Relationships and foreign keys
- Indexing strategy
- Repository pattern usage
- Query examples and optimization
- Backup and recovery procedures

**Read this for:** Understanding data model, writing queries, database operations

### [CONTRIBUTING.md](CONTRIBUTING.md)
**Development guide and coding standards**

- Development environment setup
- Code organization and structure
- C# and Vue 3 coding standards
- Testing (unit and integration)
- Git workflow and commit conventions
- Pull request process
- Common development tasks
- Troubleshooting development issues

**Read this for:** Contributing code, setting up development environment

### [SECURITY.md](SECURITY.md)
**Security implementation and best practices**

- All security features explained
- Authentication and authorization details
- Password policy configuration
- Account lockout and rate limiting
- API rate limiting configuration
- CORS protection
- JWT token security
- Audit logging
- Pre-production security checklist
- Operational security practices
- Key rotation procedures
- Power BI service principal least privilege

**Read this for:** Security understanding, deployment security planning, compliance

### [deployment/DEPLOYMENT.md](deployment/DEPLOYMENT.md)
**Production deployment guide**

- Prerequisites and quick start
- Docker Compose setup
- HTTPS configuration (Caddy, nginx)
- Environment variables
- Power BI integration setup
- Azure AD app registration
- Security settings and checklist
- Health checks and verification

**Read this for:** Deploying to production, configuring Power BI, setting up HTTPS

### [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
**Operations, troubleshooting, and maintenance guide**

- Operational tasks (daily, weekly, monthly)
- Health monitoring and metrics
- Common issues with step-by-step solutions:
  - Application won't start
  - Rate limiting (429) errors
  - CORS errors
  - Account locked / can't login
  - Database corruption
  - Memory usage issues
  - Slow responses
  - SSL/HTTPS issues
  - Power BI integration issues
- Performance tuning
- Monitoring best practices
- Disaster recovery and backups

**Read this for:** Solving problems, monitoring, maintenance tasks

### [ROADMAP.md](ROADMAP.md)
**Feature roadmap and implementation plans**

- Current version (v0.3.0) highlights
- Planned phases (v0.4.0 through v0.8.0+)
- Detailed feature descriptions
- Implementation details and components
- Acceptance criteria
- Cross-phase features (security, performance, docs)
- Release schedule
- Contribution opportunities

**Read this for:** Understanding future direction, feature planning, contributing ideas

### [SECURITY.md](SECURITY.md)
**Security implementation details and best practices**

(See above for detailed description)

### [deployment/RUNBOOK_LOGGING.md](deployment/RUNBOOK_LOGGING.md)
**Observability, logging, and alerting**

- Structured logging with Serilog
- JSON log format and enrichers
- Log collection and aggregation
- Metrics exposure (Prometheus)
- Health checks and readiness probes
- Alert thresholds and configuration
- Troubleshooting workflow

**Read this for:** Setting up logging infrastructure, configuring alerts

### [RELEASE_NOTES.md](documentation/RELEASE_NOTES.md)
**Latest release highlights and upgrade notes**

- Current version highlights
- Key features in this release
- Upgrade notes and migration paths
- Breaking changes (if any)

**Read this for:** Understanding what's new in current version

### [CHANGELOG.md](CHANGELOG.md)
**Complete version history and release notes**

- All releases in chronological order
- Features added, fixed, changed
- Deprecations and breaking changes
- Format follows "Keep a Changelog" standard

**Read this for:** Historical context, finding when features were added

---

## 🎯 Documentation Quality Standards

All documentation in PBIHoster follows these principles:

- **Clear Structure**: Headings, tables, and lists for scannability
- **Complete**: Covers all necessary aspects of the topic
- **Current**: Updated with every release
- **Practical**: Includes examples and step-by-step instructions
- **Linked**: Cross-references to related documentation
- **Role-Specific**: Content appropriate for the intended audience
- **Searchable**: Uses consistent terminology and keywords

---

## 📋 Documentation Maintenance

### Who Updates Documentation?
- **Contributors**: Update CONTRIBUTING.md and code-related docs
- **Release Manager**: Updates CHANGELOG.md and documentation/RELEASE_NOTES.md
- **Product Manager**: Maintains ROADMAP.md
- **DevOps/Operations**: Updates DEPLOYMENT.md and TROUBLESHOOTING.md
- **Security**: Maintains SECURITY.md

### When to Update?
- Before every release (CHANGELOG.md, documentation/RELEASE_NOTES.md)
- When architecture changes (ARCHITECTURE.md, DATABASE.md)
- When APIs are added/changed (API.md)
- When features are added/planned (ROADMAP.md)
- When issues are discovered (TROUBLESHOOTING.md)
- When security practices change (SECURITY.md)

### Documentation Review
- All documentation changes reviewed in PRs
- Accuracy verified against implementation
- Examples tested and verified to work

---

## 🔍 Finding Information

### Searching Documentation
- Use your IDE/editor's search (Ctrl+F / Cmd+F) within a document
- Use GitHub's search across all files
- Use the Table of Contents at the top of each document
- Check the "Quick Links" section in README.md

### Common Questions & Answers

**Q: How do I deploy to production?**  
A: See [deployment/DEPLOYMENT.md](deployment/DEPLOYMENT.md)

**Q: How do I add a new API endpoint?**  
A: See [CONTRIBUTING.md](CONTRIBUTING.md#adding-a-new-api-endpoint)

**Q: How do I set up Power BI integration?**  
A: See [deployment/DEPLOYMENT.md](deployment/DEPLOYMENT.md#power-bi-integration)

**Q: What are the security requirements?**  
A: See [SECURITY.md](SECURITY.md) and [deployment/DEPLOYMENT.md](deployment/DEPLOYMENT.md#essential-security-settings)

**Q: How do I troubleshoot login issues?**  
A: See [TROUBLESHOOTING.md](TROUBLESHOOTING.md#account-locked---cant-login)

**Q: What's the system architecture?**  
A: See [ARCHITECTURE.md](ARCHITECTURE.md)

**Q: How do I contribute code?**  
A: See [CONTRIBUTING.md](CONTRIBUTING.md)

---

## 📞 Getting Help

If documentation is unclear or missing:
1. **GitHub Issues**: [Report a documentation bug](https://github.com/aenas11/pbihoster/issues)
2. **GitHub Discussions**: [Ask a question](https://github.com/aenas11/pbihoster/discussions)
3. **Pull Requests**: Submit improvements to documentation

---

**Last Updated**: 2025-02-06  
**Documentation Version**: Aligned with v0.3.0
