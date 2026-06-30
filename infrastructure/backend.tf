terraform {
  backend "s3" {
    bucket         = "guidedmentor-terraform-state"
    key            = "infrastructure/terraform.tfstate"
    region         = "ap-southeast-2"
    dynamodb_table = "guidedmentor-terraform-locks"
    encrypt        = true
  }
}
