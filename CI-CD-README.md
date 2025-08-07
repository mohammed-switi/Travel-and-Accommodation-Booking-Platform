# CI/CD Pipeline Setup for Travel and Accommodation Booking Platform

This document outlines the setup and configuration required for the CI/CD pipeline deployment on AWS.

## 🏗️ Architecture Overview

The CI/CD pipeline consists of:
- **GitHub Actions** for CI/CD orchestration
- **Amazon ECR** for container image storage
- **Amazon ECS** with Fargate for container orchestration
- **Application Load Balancer** for traffic distribution
- **RDS** for database hosting
- **ElastiCache** for Redis caching

## 📋 Prerequisites

### 1. AWS Resources Setup

#### Create ECR Repository
```bash
aws ecr create-repository --repository-name travel-booking-platform --region us-east-1
```

#### Create ECS Cluster
```bash
aws ecs create-cluster --cluster-name travel-booking-cluster-development
aws ecs create-cluster --cluster-name travel-booking-cluster-staging  
aws ecs create-cluster --cluster-name travel-booking-cluster-production
```

#### Create CloudWatch Log Group
```bash
aws logs create-log-group --log-group-name /ecs/travel-booking-platform
```

### 2. IAM Roles

#### ECS Task Execution Role
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Service": "ecs-tasks.amazonaws.com"
      },
      "Action": "sts:AssumeRole"
    }
  ]
}
```

#### ECS Task Role (for application permissions)
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "ssm:GetParameter",
        "ssm:GetParameters",
        "ssm:GetParametersByPath"
      ],
      "Resource": "arn:aws:ssm:*:*:parameter/travel-booking/*"
    }
  ]
}
```

### 3. GitHub Secrets Configuration

Add the following secrets to your GitHub repository:

#### AWS Configuration
- `AWS_ACCESS_KEY_ID` - AWS access key
- `AWS_SECRET_ACCESS_KEY` - AWS secret key  
- `AWS_SESSION_TOKEN` - AWS session token (if using temporary credentials)
- `AWS_REGION` - AWS region (e.g., us-east-1)

#### Email Notifications (Optional)
- `SMTP_SERVER` - SMTP server address
- `SMTP_PORT` - SMTP port (usually 587)
- `SMTP_USERNAME` - SMTP username
- `SMTP_PASSWORD` - SMTP password
- `EMAIL_FROM` - Sender email address
- `EMAIL_TO` - Recipient email address

### 4. AWS Systems Manager Parameters

Store sensitive configuration in AWS Parameter Store:

```bash
# Database connection string
aws ssm put-parameter --name "/travel-booking/development/database-connection-string" \
  --value "Server=your-rds-endpoint;Database=TravelBookingDB;User Id=dbuser;Password=dbpassword;" \
  --type "SecureString"

# Redis connection string  
aws ssm put-parameter --name "/travel-booking/development/redis-connection-string" \
  --value "your-elasticache-endpoint:6379" \
  --type "SecureString"

# JWT Secret
aws ssm put-parameter --name "/travel-booking/development/jwt-secret" \
  --value "your-jwt-secret-key-here" \
  --type "SecureString"
```

Repeat for `staging` and `production` environments.

## 🚀 Deployment Process

### 1. ECS Service Creation

Create ECS services for each environment:

```bash
aws ecs create-service \
  --cluster travel-booking-cluster-development \
  --service-name travel-booking-service-development \
  --task-definition travel-booking-platform:1 \
  --desired-count 1 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-12345],securityGroups=[sg-12345],assignPublicIp=ENABLED}"
```

### 2. Load Balancer Integration

1. Create Application Load Balancer
2. Create target group pointing to ECS service
3. Configure health checks on `/health` endpoint
4. Set up listener rules for routing

### 3. Database Setup

1. Create RDS instance (SQL Server)
2. Configure security groups for ECS access
3. Run database migrations:
   ```bash
   dotnet ef database update
   ```

### 4. Redis Setup

1. Create ElastiCache Redis cluster
2. Configure security groups for ECS access

## 🔄 Pipeline Workflow

### On Pull Request:
1. **Build & Test** - Compiles code and runs unit tests
2. **Code Coverage** - Generates coverage reports

### On Push to develop/staging/main:
1. **Build & Test** - Compiles code and runs unit tests
2. **Docker Build & Push** - Builds container image and pushes to ECR
3. **Deploy to ECS** - Updates ECS service with new image
4. **Health Check** - Waits for deployment to stabilize
5. **Notification** - Sends email notification on success/failure

## 🏠 Local Development

### Using Docker Compose

1. **Start all services:**
   ```bash
   docker-compose up -d
   ```

2. **With Nginx proxy:**
   ```bash
   docker-compose --profile with-proxy up -d
   ```

3. **View logs:**
   ```bash
   docker-compose logs -f travel-booking-api
   ```

4. **Stop services:**
   ```bash
   docker-compose down
   ```

### Direct Docker Build

```bash
# Build image
docker build -t travel-booking-platform .

# Run container
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development travel-booking-platform
```

## 🔧 Configuration

### Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | Yes |
| `ConnectionStrings__DefaultConnection` | Database connection | Yes |
| `ConnectionStrings__RedisConnection` | Redis connection | Yes |
| `JwtSettings__SecretKey` | JWT signing key | Yes |

### Health Check Endpoint

The application exposes a health check endpoint at `/health` that returns:
- `200 OK` - Service is healthy
- `503 Service Unavailable` - Service is unhealthy

## 🛡️ Security Best Practices

1. **Secrets Management** - Use AWS Parameter Store for sensitive data
2. **IAM Roles** - Use least-privilege access principles
3. **Container Security** - Run containers as non-root user
4. **Network Security** - Use VPC and security groups properly
5. **Image Scanning** - Enable ECR vulnerability scanning

## 📊 Monitoring & Logging

- **CloudWatch Logs** - Application logs are sent to CloudWatch
- **CloudWatch Metrics** - ECS and application metrics
- **Health Checks** - ECS and ALB health monitoring
- **Email Notifications** - Build and deployment status

## 🐛 Troubleshooting

### Common Issues

1. **ECS Task Won't Start**
   - Check CloudWatch logs for errors
   - Verify task definition parameters
   - Ensure security groups allow traffic

2. **Database Connection Issues**
   - Verify Parameter Store values
   - Check security group rules
   - Test network connectivity

3. **Docker Build Fails**
   - Check Dockerfile syntax
   - Verify base image availability
   - Review build logs in GitHub Actions

### Useful Commands

```bash
# View ECS service status
aws ecs describe-services --cluster travel-booking-cluster-development --services travel-booking-service-development

# Check task logs
aws logs tail /ecs/travel-booking-platform --follow

# Validate task definition
aws ecs describe-task-definition --task-definition travel-booking-platform

# Force new deployment
aws ecs update-service --cluster travel-booking-cluster-development --service travel-booking-service-development --force-new-deployment
```

## 📝 Notes

- The pipeline is triggered on pushes to `develop`, `staging`, and `main` branches
- Each branch corresponds to a different environment deployment
- The Docker image is tagged with the Git commit SHA for traceability
- Email notifications require SMTP configuration in GitHub Secrets
